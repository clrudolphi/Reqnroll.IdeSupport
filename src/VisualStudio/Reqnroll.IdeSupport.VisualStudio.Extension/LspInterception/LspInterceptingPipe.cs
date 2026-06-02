using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Reqnroll.IdeSupport.VisualStudio.Extension.LspInterception;

/// <summary>
/// Inserts an interception pipeline between VS and the LSP server process.
/// </summary>
/// <remarks>
/// <para>
/// Sits in the middle of the LSP stdio channel:
/// <code>
///   VS  ──write──► [VS-facing PipeReader/Writer]
///                        │                  ▲
///                  SendPump task      ReceivePump task
///                        │                  │
///                        ▼                  │
///              Server stdin PipeWriter   Server stdout PipeReader
/// </code>
/// </para>
/// <para>
/// Each pump reads raw LSP frames (<c>Content-Length: N\r\n\r\nBODY</c>), parses them to
/// <see cref="LspMessage"/>, runs the relevant interceptor list, then — if no interceptor
/// consumed the message — re-encodes and forwards.
/// </para>
/// </remarks>
internal sealed class LspInterceptingPipe : IDisposable
{
    private static readonly Encoding Utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

    private readonly IDuplexPipe                       _serverPipe;
    private readonly IReadOnlyList<ILspMessageInterceptor> _sendInterceptors;
    private readonly IReadOnlyList<ILspMessageInterceptor> _receiveInterceptors;
    private readonly TraceSource                       _traceSource;

    // The two Pipe objects whose Reader/Writer ends form the VS-facing IDuplexPipe.
    // VS reads from _toVsPipe.Reader; VS writes to _fromVsPipe.Writer.
    private readonly Pipe _toVsPipe    = new Pipe();   // server → VS direction
    private readonly Pipe _fromVsPipe  = new Pipe();   // VS → server direction

    // Serialises injected writes against the send pump so frames are not interleaved.
    private readonly SemaphoreSlim _injectLock = new SemaphoreSlim(1, 1);

    private readonly CancellationTokenSource _cts = new CancellationTokenSource();
    private CancellationTokenSource? _linkedCts;
    private Task? _sendPump;
    private Task? _receivePump;
    private bool _disposed;

    /// <summary>
    /// Initialises the intercepting pipe but does not start pumping yet.
    /// Call <see cref="StartAsync"/> to begin.
    /// </summary>
    /// <param name="serverPipe">
    /// The raw <see cref="IDuplexPipe"/> connected to the server process's stdio.
    /// </param>
    /// <param name="sendInterceptors">
    /// Interceptors applied to messages travelling VS → Server.
    /// </param>
    /// <param name="receiveInterceptors">
    /// Interceptors applied to messages travelling Server → VS.
    /// </param>
    /// <param name="traceSource">Trace sink for pump-level diagnostics.</param>
    public LspInterceptingPipe(
        IDuplexPipe serverPipe,
        IReadOnlyList<ILspMessageInterceptor> sendInterceptors,
        IReadOnlyList<ILspMessageInterceptor> receiveInterceptors,
        TraceSource traceSource)
    {
        _serverPipe          = serverPipe          ?? throw new ArgumentNullException(nameof(serverPipe));
        _sendInterceptors    = sendInterceptors    ?? throw new ArgumentNullException(nameof(sendInterceptors));
        _receiveInterceptors = receiveInterceptors ?? throw new ArgumentNullException(nameof(receiveInterceptors));
        _traceSource         = traceSource         ?? throw new ArgumentNullException(nameof(traceSource));

        // VS reads from _toVsPipe.Reader and writes to _fromVsPipe.Writer.
        VsFacingPipe = new DuplexPipeAdapter(_toVsPipe.Reader, _fromVsPipe.Writer);
    }

    /// <summary>
    /// The <see cref="IDuplexPipe"/> to hand to VS (<c>CreateServerConnectionAsync</c> return value).
    /// </summary>
    public IDuplexPipe VsFacingPipe { get; }

    /// <summary>
    /// Starts the two background pump tasks.  Returns immediately; pumps run until
    /// the connection closes or <see cref="Dispose"/> is called.
    /// </summary>
    public Task StartAsync(CancellationToken externalCancellation)
    {
        _linkedCts = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token, externalCancellation);
        var ct     = _linkedCts.Token;

        // VS writes to _fromVsPipe.Writer → SendPump reads _fromVsPipe.Reader → server stdin
        _sendPump = PumpAsync(
            source:       _fromVsPipe.Reader,
            destination:  _serverPipe.Output,
            interceptors: _sendInterceptors,
            direction:    LspMessageDirection.Send,
            ct:           ct);

        // Server stdout → ReceivePump reads _serverPipe.Input → _toVsPipe.Writer → VS reads _toVsPipe.Reader
        _receivePump = PumpAsync(
            source:       _serverPipe.Input,
            destination:  _toVsPipe.Writer,
            interceptors: _receiveInterceptors,
            direction:    LspMessageDirection.Receive,
            ct:           ct);

        return Task.CompletedTask;
    }

    // ── Core pump loop ──────────────────────────────────────────────────────

    private async Task PumpAsync(
        PipeReader                             source,
        PipeWriter                             destination,
        IReadOnlyList<ILspMessageInterceptor>  interceptors,
        LspMessageDirection                    direction,
        CancellationToken                      ct)
    {
        try
        {
            while (!ct.IsCancellationRequested)
            {
                var frame = await ReadNextFrameAsync(source, ct).ConfigureAwait(false);

                if (frame is null)
                    break;

                if (frame.Body is null)
                {
                    // Malformed JSON — forward raw bytes verbatim so the connection stays alive.
                    await WriteFrameAsync(destination, frame.RawBytes, ct).ConfigureAwait(false);
                    continue;
                }

                var message = new LspMessage(direction, frame.Body, DateTimeOffset.Now);
                var result  = await RunInterceptorsAsync(message, interceptors, ct).ConfigureAwait(false);

                if (result == LspInterceptorResult.PassThrough)
                    await WriteFrameAsync(destination, frame.RawBytes, ct).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) { /* normal shutdown */ }
        catch (Exception ex)
        {
            _traceSource.TraceEvent(TraceEventType.Error, 0,
                "LspInterceptingPipe [{0}] pump faulted: {1}", direction, ex);
        }
        finally
        {
            await destination.CompleteAsync().ConfigureAwait(false);
        }
    }

    // ── LSP frame reader ────────────────────────────────────────────────────

    private sealed class LspFrame
    {
        public LspFrame(JObject? body, byte[] rawBytes) { Body = body; RawBytes = rawBytes; }
        public JObject? Body    { get; }
        public byte[]   RawBytes { get; }
    }

    /// <summary>
    /// Reads one LSP frame from <paramref name="reader"/>.
    /// Returns <c>null</c> when the pipe is completed (remote side closed).
    /// Returns an <see cref="LspFrame"/> with a <c>null</c> <see cref="LspFrame.Body"/> when
    /// JSON parsing fails; raw bytes are still present so the caller can forward verbatim.
    /// </summary>
    private static async Task<LspFrame?> ReadNextFrameAsync(PipeReader reader, CancellationToken ct)
    {
        // Phase 1 – read until we see \r\n\r\n and can extract Content-Length.
        // We use AdvanceTo(consumed, examined) correctly: we only mark bytes as consumed
        // once we know exactly which bytes belong to the header vs. the body.
        int contentLength;
        int headerLength; // total byte length of "Content-Length: N\r\n\r\n"

        while (true)
        {
            var result = await reader.ReadAsync(ct).ConfigureAwait(false);
            var buffer = result.Buffer;

            if (result.IsCompleted && buffer.IsEmpty)
                return null;

            if (TryParseHeader(buffer, out contentLength, out headerLength))
            {
                // Mark exactly the header bytes as consumed; leave body bytes in the pipe.
                reader.AdvanceTo(buffer.GetPosition(headerLength));
                break;
            }

            // Haven't seen the full header yet – tell the pipe we've examined everything
            // but consumed nothing so it can give us more data next time.
            reader.AdvanceTo(buffer.Start, buffer.End);

            if (result.IsCompleted)
                return null; // pipe ended mid-header
        }

        // Phase 2 – read exactly contentLength body bytes.
        var bodyBytes = await ReadExactAsync(reader, contentLength, ct).ConfigureAwait(false);
        if (bodyBytes is null)
            return null;

        // Re-build raw frame for verbatim forwarding.
        var headerText = $"Content-Length: {contentLength}\r\n\r\n";
        var headerEnc  = Utf8NoBom.GetBytes(headerText);
        var rawBytes   = new byte[headerEnc.Length + bodyBytes.Length];
        Array.Copy(headerEnc, 0, rawBytes, 0, headerEnc.Length);
        Array.Copy(bodyBytes, 0, rawBytes, headerEnc.Length, bodyBytes.Length);

        JObject? body;
        try
        {
            body = JObject.Parse(Utf8NoBom.GetString(bodyBytes));
        }
        catch (Exception)
        {
            body = null; // malformed JSON — caller forwards raw bytes without intercepting
        }

        return new LspFrame(body, rawBytes);
    }

    /// <summary>
    /// Tries to find the LSP header block (terminated by <c>\r\n\r\n</c>) in
    /// <paramref name="buffer"/> and extract the <c>Content-Length</c> value.
    /// </summary>
    private static bool TryParseHeader(ReadOnlySequence<byte> buffer, out int contentLength, out int headerLength)
    {
        contentLength = 0;
        headerLength  = 0;

        // Flatten to a single array only if the buffer is multi-segment (rare for small headers).
        var bytes = buffer.IsSingleSegment
            ? buffer.First.Span.ToArray()
            : buffer.ToArray();

        for (int i = 0; i <= bytes.Length - 4; i++)
        {
            if (bytes[i] == '\r' && bytes[i + 1] == '\n' &&
                bytes[i + 2] == '\r' && bytes[i + 3] == '\n')
            {
                var headerText = Utf8NoBom.GetString(bytes, 0, i);
                foreach (var line in headerText.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries))
                {
                    if (line.StartsWith("Content-Length:", StringComparison.OrdinalIgnoreCase))
                    {
                        var valueStr = line.Substring("Content-Length:".Length).Trim();
                        if (int.TryParse(valueStr, out contentLength))
                        {
                            headerLength = i + 4; // header bytes + \r\n\r\n
                            return true;
                        }
                    }
                }
            }
        }

        return false;
    }

    /// <summary>Reads exactly <paramref name="count"/> bytes from <paramref name="reader"/>.</summary>
    private static async Task<byte[]?> ReadExactAsync(PipeReader reader, int count, CancellationToken ct)
    {
        var accumulator = new List<byte>(count);

        while (accumulator.Count < count)
        {
            var result = await reader.ReadAsync(ct).ConfigureAwait(false);
            var buffer = result.Buffer;

            if (result.IsCompleted && buffer.IsEmpty)
                return null;

            int needed = count - accumulator.Count;
            var slice  = buffer.Length >= needed ? buffer.Slice(0, needed) : buffer;

            foreach (var seg in slice)
            {
                var arr = seg.ToArray();
                foreach (var b in arr)
                    accumulator.Add(b);
            }

            reader.AdvanceTo(slice.End);
        }

        return accumulator.ToArray();
    }

    // ── Frame writer ────────────────────────────────────────────────────────

    private static async Task WriteFrameAsync(PipeWriter writer, byte[] rawFrame, CancellationToken ct)
    {
        var memory = writer.GetMemory(rawFrame.Length);
        rawFrame.CopyTo(memory);
        writer.Advance(rawFrame.Length);
        await writer.FlushAsync(ct).ConfigureAwait(false);
    }

    // ── Interceptor pipeline ────────────────────────────────────────────────

    private async Task<LspInterceptorResult> RunInterceptorsAsync(
        LspMessage                            message,
        IReadOnlyList<ILspMessageInterceptor> interceptors,
        CancellationToken                     ct)
    {
        foreach (var interceptor in interceptors)
        {
            try
            {
                var result = await interceptor.InterceptAsync(message, ct).ConfigureAwait(false);
                if (result == LspInterceptorResult.Consume)
                    return LspInterceptorResult.Consume;
            }
            catch (Exception ex)
            {
                _traceSource.TraceEvent(TraceEventType.Warning, 0,
                    "LspInterceptingPipe: Interceptor '{0}' threw: {1}",
                    interceptor.GetType().Name, ex.Message);
            }
        }

        return LspInterceptorResult.PassThrough;
    }

    // ── Notification injection (VS → Server) ───────────────────────────────

    /// <summary>
    /// Encodes a JSON-RPC notification and writes it directly into the server-bound
    /// output stream, bypassing the VS-facing pipe.  Safe to call from any thread;
    /// uses <see cref="_injectLock"/> to serialise against the send pump.
    /// </summary>
    /// <param name="method">LSP method name, e.g. <c>reqnroll/projectLoaded</c>.</param>
    /// <param name="paramsJson">
    /// Already-serialized JSON string for the <c>params</c> field, or <c>null</c>/empty
    /// to omit the field.
    /// </param>
    public async Task SendNotificationToServerAsync(
        string method,
        string? paramsJson,
        CancellationToken cancellationToken)
    {
        if (_disposed) return;

        // Build the JSON-RPC notification frame.
        var body = string.IsNullOrEmpty(paramsJson)
            ? $"{{\"jsonrpc\":\"2.0\",\"method\":{JsonEscape(method)}}}"
            : $"{{\"jsonrpc\":\"2.0\",\"method\":{JsonEscape(method)},\"params\":{paramsJson}}}";

        var bodyBytes  = Utf8NoBom.GetBytes(body);
        var headerText = $"Content-Length: {bodyBytes.Length}\r\n\r\n";
        var headerBytes = Utf8NoBom.GetBytes(headerText);

        await _injectLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var memory = _serverPipe.Output.GetMemory(headerBytes.Length + bodyBytes.Length);
            headerBytes.CopyTo(memory);
            bodyBytes.CopyTo(memory.Slice(headerBytes.Length));
            _serverPipe.Output.Advance(headerBytes.Length + bodyBytes.Length);
            await _serverPipe.Output.FlushAsync(cancellationToken).ConfigureAwait(false);

            _traceSource.TraceInformation(
                "LspInterceptingPipe: Injected notification '{0}' ({1} bytes)", method, bodyBytes.Length);
        }
        finally
        {
            _injectLock.Release();
        }
    }

    private static string JsonEscape(string value)
        => Newtonsoft.Json.JsonConvert.ToString(value); // produces "\"value\""

    // ── IDisposable ─────────────────────────────────────────────────────────

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _cts.Cancel();
        _linkedCts?.Cancel();
        _linkedCts?.Dispose();
        _cts.Dispose();
        _injectLock.Dispose();

        _toVsPipe.Writer.Complete();
        _fromVsPipe.Writer.Complete();
    }

    // ── Inner helper ────────────────────────────────────────────────────────

    /// <summary>Adapts a <see cref="PipeReader"/> / <see cref="PipeWriter"/> pair into an <see cref="IDuplexPipe"/>.</summary>
    private sealed class DuplexPipeAdapter : IDuplexPipe
    {
        public DuplexPipeAdapter(PipeReader input, PipeWriter output)
        {
            Input  = input;
            Output = output;
        }

        public PipeReader Input  { get; }
        public PipeWriter Output { get; }
    }
}
