using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Reqnroll.IdeSupport.VisualStudio.Extension.LspInterception;

/// <summary>
/// An <see cref="ILspMessageInterceptor"/> that writes every LSP message to a shared log
/// file in the format consumed by the
/// <see href="https://microsoft.github.io/language-server-protocol/inspector/">LSP Inspector</see>.
/// Always returns <see cref="LspInterceptorResult.PassThrough"/>.
/// </summary>
internal sealed class LspInspectorLogger : ILspMessageInterceptor, IDisposable
{
    private readonly string _logFilePath;
    private readonly TraceSource _traceSource;
    private readonly SemaphoreSlim _gate = new SemaphoreSlim(1, 1);

    private StreamWriter? _writer;
    private bool _disposed;

    /// <summary>
    /// Initialises the logger and creates (or truncates) the log file at
    /// <paramref name="logFilePath"/>.
    /// </summary>
    public LspInspectorLogger(string logFilePath, TraceSource traceSource)
    {
        _logFilePath = logFilePath ?? throw new ArgumentNullException(nameof(logFilePath));
        _traceSource = traceSource ?? throw new ArgumentNullException(nameof(traceSource));

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(logFilePath)!);
            _writer = new StreamWriter(
                new FileStream(logFilePath, FileMode.Create, FileAccess.Write, FileShare.Read),
                Encoding.UTF8,
                bufferSize: 4096,
                leaveOpen: false);
            _writer.AutoFlush = true;
            _traceSource.TraceInformation("LspInspectorLogger: Writing LSP Inspector log to '{0}'.", logFilePath);
        }
        catch (Exception ex)
        {
            _traceSource.TraceEvent(TraceEventType.Error, 0,
                "LspInspectorLogger: Failed to open log file '{0}': {1}", logFilePath, ex.Message);
        }
    }

    /// <inheritdoc/>
    public async Task<LspInterceptorResult> InterceptAsync(LspMessage message, CancellationToken cancellationToken)
    {
        if (_writer is null || _disposed)
            return LspInterceptorResult.PassThrough;

        await _gate.WaitAsync().ConfigureAwait(false);
        try
        {
            _writer.Write(FormatEntry(message));
            _writer.Flush();
        }
        catch (Exception ex)
        {
            _traceSource.TraceEvent(TraceEventType.Warning, 0,
                "LspInspectorLogger: Error writing log entry: {0}", ex.Message);
        }
        finally
        {
            _gate.Release();
        }

        return LspInterceptorResult.PassThrough;
    }

    // ── Formatting ─────────────────────────────────────────────────────────

    private static string FormatEntry(LspMessage msg)
    {
        // lsp-viewer format: [LSP   - HH:mm:ss] <JSON>\n
        // JSON: {"isLSPMessage":true,"type":"<type>","message":{...},"timestamp":<unix-ms>}

        string type;
        bool isSend = msg.Direction == LspMessageDirection.Send;
        if (msg.IsRequest)
            type = isSend ? "send-request" : "receive-request";
        else if (msg.IsResponse)
            type = isSend ? "send-response" : "receive-response";
        else
            type = isSend ? "send-notification" : "receive-notification";

        long timestampMs = msg.Timestamp.ToUnixTimeMilliseconds();
        string timeLabel = msg.Timestamp.LocalDateTime.ToString("HH:mm:ss");

        var entry = new JObject
        {
            ["isLSPMessage"] = true,
            ["type"]         = type,
            ["message"]      = msg.Body,
            ["timestamp"]    = timestampMs,
        };

        // Compact single-line JSON to match the lsp-viewer expectation.
        string json = JsonConvert.SerializeObject(entry, Formatting.None);
        return $"[LSP   - {timeLabel}] {json}\n";
    }

    // ── IDisposable

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _gate.Wait(millisecondsTimeout: 500);
        try
        {
            _writer?.Flush();
            _writer?.Dispose();
        }
        catch { /* best-effort */ }
        _gate.Release();
        _gate.Dispose();
    }
}
