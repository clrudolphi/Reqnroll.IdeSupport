#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Nerdbank.Streams;
using OmniSharp.Extensions.LanguageServer.Client;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Client;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Server;
using Reqnroll.IdeSupport.LSP.Server.Hosting;

namespace Reqnroll.IdeSupport.LSP.Server.Benchmarks.Harness;

/// <summary>
/// Hosts the <em>real</em> Reqnroll LSP server in-process over an in-memory full-duplex pipe and
/// connects an OmniSharp client, exposing timed request round-trips and timestamped server-push
/// capture. This is the §9 Layer 2 benchmark driver — it generalises the spec suite's
/// <c>LspServerHarness</c> to measure latency at the protocol boundary (serialize → transport →
/// handler → transport → deserialize), which is what the "from last didChange" targets require.
/// </summary>
public sealed class BenchmarkLspHarness : IAsyncDisposable
{
    private IDisposable? _server;
    private ILanguageClient? _client;

    private readonly object _diagLock = new();
    private readonly Dictionary<string, long> _lastDiagTimestamp = new(StringComparer.Ordinal);

    public ILanguageClient Client =>
        _client ?? throw new InvalidOperationException("Harness not started.");

    public async Task StartAsync(string workspaceFolder, string? ideId = null)
    {
        var (serverStream, clientStream) = FullDuplexStream.CreatePair();

        var serverTask = LanguageServer.From(options =>
        {
            options.WithInput(serverStream).WithOutput(serverStream);
            Program.ConfigureServer(options, ideId);
        });

        _client = await LanguageClient.From(options =>
        {
            options.WithInput(clientStream).WithOutput(clientStream);
            options.WithRootUri(DocumentUri.FromFileSystemPath(workspaceFolder));
            options.WithWorkspaceFolder(DocumentUri.FromFileSystemPath(workspaceFolder), "benchmark-workspace");

            // Timestamp every publishDiagnostics push so a didChange can be timed against the
            // resulting diagnostics for the matching URI.
            options.OnNotification("textDocument/publishDiagnostics", (PublishDiagnosticsParams p) =>
            {
                lock (_diagLock)
                    _lastDiagTimestamp[p.Uri.ToString()] = Stopwatch.GetTimestamp();
                return Task.CompletedTask;
            });
        }).ConfigureAwait(false);

        _server = await serverTask.ConfigureAwait(false);
    }

    // ── Document lifecycle ──────────────────────────────────────────────────────

    public void OpenFeature(DocumentUri uri, int version, string text) =>
        Client.SendNotification("textDocument/didOpen", new DidOpenTextDocumentParams
        {
            TextDocument = new TextDocumentItem
            { Uri = uri, Version = version, LanguageId = "gherkin", Text = text }
        });

    public void ChangeFeature(DocumentUri uri, int version, string text) =>
        Client.SendNotification("textDocument/didChange", new DidChangeTextDocumentParams
        {
            TextDocument = new OptionalVersionedTextDocumentIdentifier { Uri = uri, Version = version },
            ContentChanges = new Container<TextDocumentContentChangeEvent>(
                new TextDocumentContentChangeEvent { Text = text })
        });

    public void OpenCSharp(DocumentUri uri, int version, string text) =>
        Client.SendNotification("textDocument/didOpen", new DidOpenTextDocumentParams
        {
            TextDocument = new TextDocumentItem
            { Uri = uri, Version = version, LanguageId = "csharp", Text = text }
        });

    public void SendNotification(string method, object payload) => Client.SendNotification(method, payload);

    // ── Timed request ───────────────────────────────────────────────────────────

    /// <summary>
    /// Sends a request and returns the wall-clock round-trip in milliseconds (the response is
    /// awaited but discarded; scenarios that need the payload use <see cref="RequestAsync{T}"/>).
    /// </summary>
    public async Task<double> TimeRequestAsync<TResp>(string method, object @params, CancellationToken ct = default)
    {
        var start = Stopwatch.GetTimestamp();
        await Client.SendRequest(method, @params).Returning<TResp>(ct).ConfigureAwait(false);
        return Stopwatch.GetElapsedTime(start).TotalMilliseconds;
    }

    public Task<TResp> RequestAsync<TResp>(string method, object @params, CancellationToken ct = default) =>
        Client.SendRequest(method, @params).Returning<TResp>(ct);

    // ── Diagnostics push timing ─────────────────────────────────────────────────

    /// <summary>
    /// Waits until a <c>publishDiagnostics</c> for <paramref name="uri"/> arrives after
    /// <paramref name="sinceTimestamp"/> (a <see cref="Stopwatch.GetTimestamp"/> value), and returns
    /// the elapsed milliseconds from that origin to the push. Returns <c>null</c> on timeout.
    /// </summary>
    public async Task<double?> WaitForDiagnosticsAsync(DocumentUri uri, long sinceTimestamp, int timeoutMs = 5000)
    {
        var key = uri.ToString();
        var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
        while (DateTime.UtcNow < deadline)
        {
            lock (_diagLock)
            {
                if (_lastDiagTimestamp.TryGetValue(key, out var ts) && ts >= sinceTimestamp)
                    return Stopwatch.GetElapsedTime(sinceTimestamp, ts).TotalMilliseconds;
            }
            await Task.Delay(10).ConfigureAwait(false);
        }
        return null;
    }

    public async ValueTask DisposeAsync()
    {
        try { (_client as IDisposable)?.Dispose(); } catch { }
        try { _server?.Dispose(); } catch { }
        await Task.CompletedTask.ConfigureAwait(false);
    }
}
