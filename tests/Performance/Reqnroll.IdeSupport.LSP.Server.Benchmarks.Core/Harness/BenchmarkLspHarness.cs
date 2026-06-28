#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
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
/// Drives the <em>real</em> Reqnroll LSP server through an OmniSharp client, exposing timed request
/// round-trips and timestamped server-push capture. Two transports:
/// <list type="bullet">
///   <item><see cref="StartAsync"/> — hosts the server <b>in-process</b> over an in-memory pipe
///   (fast, reproducible; no process/stdio boundary).</item>
///   <item><see cref="StartOutOfProcessAsync"/> — spawns the built server <b>exe</b> and talks to it
///   over <b>stdio</b> (the production transport; includes the real process boundary).</item>
/// </list>
/// Either way, latency is measured at the protocol boundary (serialize → transport → handler →
/// transport → deserialize), which is what the "from last didChange" targets require.
/// </summary>
public sealed class BenchmarkLspHarness : IAsyncDisposable
{
    private IDisposable? _server;
    private ILanguageClient? _client;
    private Process? _serverProcess;
    private readonly StringBuilder _serverStderr = new();
    private readonly object _stderrLock = new();

    private readonly object _diagLock = new();
    private readonly Dictionary<string, long> _lastDiagTimestamp = new(StringComparer.Ordinal);

    public ILanguageClient Client =>
        _client ?? throw new InvalidOperationException("Harness not started.");

    /// <summary>Anything the spawned server wrote to stderr (out-of-process mode), for diagnosis.</summary>
    public string ServerStandardError { get { lock (_stderrLock) return _serverStderr.ToString(); } }

    /// <summary>Hosts the server in-process over an in-memory full-duplex pipe.</summary>
    public async Task StartAsync(string workspaceFolder, string? ideId = null)
    {
        var (serverStream, clientStream) = FullDuplexStream.CreatePair();

        var serverTask = LanguageServer.From(options =>
        {
            options.WithInput(serverStream).WithOutput(serverStream);
            Program.ConfigureServer(options, ideId);
        });

        _client = await CreateClientAsync(input: clientStream, output: clientStream, workspaceFolder)
            .ConfigureAwait(false);
        _server = await serverTask.ConfigureAwait(false);
    }

    /// <summary>
    /// Spawns the built server executable and connects over its stdio — the production transport,
    /// including the real OS process boundary (no in-memory shortcut). Slower to start than
    /// <see cref="StartAsync"/>, but the numbers include cross-process stdio and process isolation.
    /// </summary>
    public async Task StartOutOfProcessAsync(string workspaceFolder, string serverExePath, string? ideId = null)
    {
        var psi = new ProcessStartInfo
        {
            FileName = serverExePath,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = Path.GetDirectoryName(serverExePath) ?? Environment.CurrentDirectory,
        };
        if (!string.IsNullOrEmpty(ideId))
        {
            psi.ArgumentList.Add("--ide");
            psi.ArgumentList.Add(ideId);
        }

        _serverProcess = Process.Start(psi)
            ?? throw new InvalidOperationException($"Failed to start server process '{serverExePath}'.");

        // Drain stderr so the server can't block on a full pipe, and keep it for diagnosis.
        _serverProcess.ErrorDataReceived += (_, e) =>
        {
            if (e.Data is not null) lock (_stderrLock) _serverStderr.AppendLine(e.Data);
        };
        _serverProcess.BeginErrorReadLine();

        // The client writes to the server's stdin and reads from its stdout; use the raw BaseStreams
        // so JSON-RPC framing isn't mangled by text-mode StreamReader/Writer translation.
        _client = await CreateClientAsync(
            input: _serverProcess.StandardOutput.BaseStream,
            output: _serverProcess.StandardInput.BaseStream,
            workspaceFolder).ConfigureAwait(false);
    }

    private async Task<ILanguageClient> CreateClientAsync(Stream input, Stream output, string workspaceFolder) =>
        await LanguageClient.From(options =>
        {
            options.WithInput(input).WithOutput(output);
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

    // ── Typed request helpers (used by the editing-session scenario's bursts) ────
    // Each takes a CancellationToken: cancelling it sends $/cancelRequest on the wire, modelling a
    // client superseding an in-flight request when the user types again.

    public Task<SemanticTokens?> RequestSemanticTokensAsync(DocumentUri uri, CancellationToken ct = default) =>
        RequestAsync<SemanticTokens?>("textDocument/semanticTokens/full",
            new SemanticTokensParams { TextDocument = new TextDocumentIdentifier { Uri = uri } }, ct);

    public Task<CompletionList?> RequestCompletionAsync(DocumentUri uri, int line, int character, CancellationToken ct = default) =>
        RequestAsync<CompletionList?>("textDocument/completion",
            new CompletionParams
            {
                TextDocument = new TextDocumentIdentifier { Uri = uri },
                Position = new Position(line, character),
            }, ct);

    public Task<LocationOrLocationLinks?> RequestDefinitionAsync(DocumentUri uri, int line, int character, CancellationToken ct = default) =>
        RequestAsync<LocationOrLocationLinks?>("textDocument/definition",
            new DefinitionParams
            {
                TextDocument = new TextDocumentIdentifier { Uri = uri },
                Position = new Position(line, character),
            }, ct);

    public Task<SymbolInformationOrDocumentSymbolContainer?> RequestDocumentSymbolAsync(DocumentUri uri, CancellationToken ct = default) =>
        RequestAsync<SymbolInformationOrDocumentSymbolContainer?>("textDocument/documentSymbol",
            new DocumentSymbolParams { TextDocument = new TextDocumentIdentifier { Uri = uri } }, ct);

    public Task<Container<FoldingRange>?> RequestFoldingRangeAsync(DocumentUri uri, CancellationToken ct = default) =>
        RequestAsync<Container<FoldingRange>?>("textDocument/foldingRange",
            new FoldingRangeRequestParam { TextDocument = new TextDocumentIdentifier { Uri = uri } }, ct);

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
        if (_serverProcess is not null)
        {
            try { if (!_serverProcess.HasExited) _serverProcess.Kill(entireProcessTree: true); } catch { }
            try { _serverProcess.Dispose(); } catch { }
        }
        await Task.CompletedTask.ConfigureAwait(false);
    }
}
