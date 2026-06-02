using Nerdbank.Streams;
using OmniSharp.Extensions.LanguageServer.Client;                      // LanguageClient factory + option extensions
using OmniSharp.Extensions.LanguageServer.Protocol;                    // WorkspaceNames, DocumentUri
using OmniSharp.Extensions.LanguageServer.Protocol.Client;             // ILanguageClient
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;// SemanticTokensWorkspaceCapability
using OmniSharp.Extensions.LanguageServer.Protocol.Models;             // InitializeResult
using OmniSharp.Extensions.LanguageServer.Server;                      // LanguageServer factory
using Reqnroll.IdeSupport.LSP.Server;
using Reqnroll.IdeSupport.LSP.Server.Services;

namespace Reqnroll.IdeSupport.LSP.Server.Specs.Support;

/// <summary>
/// Hosts the <em>real</em> Reqnroll LSP server in-process over an in-memory full-duplex pipe
/// and connects an OmniSharp <see cref="ILanguageClient"/> to it, so specs can exercise the
/// actual LSP wire protocol (initialize, didOpen, semanticTokens, custom reqnroll/* notifications,
/// workspace/semanticTokens/refresh) end-to-end.
/// </summary>
/// <remarks>
/// One harness per scenario; Reqnroll disposes it at scenario end.  The server transport is
/// supplied by the spec rather than stdio thanks to <see cref="Program.ConfigureServer"/> being
/// transport-agnostic.
/// </remarks>
public sealed class LspServerHarness : IAsyncDisposable
{
    private IDisposable? _server;
    private ILanguageClient? _client;
    private readonly object _refreshLock = new();
    private int _refreshCount;
    private TaskCompletionSource<int> _refreshSignal = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public ILanguageClient Client =>
        _client ?? throw new InvalidOperationException("Harness not started.");

    /// <summary>The InitializeResult returned by the server (capabilities, server info).</summary>
    public InitializeResult ServerInitializeResult => Client.ServerSettings;

    /// <summary>Number of workspace/semanticTokens/refresh requests received so far.</summary>
    public int RefreshCount { get { lock (_refreshLock) return _refreshCount; } }

    public async Task StartAsync(string workspaceFolder, string? ideId = null)
    {
        var (serverStream, clientStream) = FullDuplexStream.CreatePair();

        var profile = SemanticTokenProfileFactory.Create(ideId);

        // Start the server first (do not await yet — From() completes once the client's
        // initialize handshake lands).
        var serverTask = LanguageServer.From(options =>
        {
            options.WithInput(serverStream).WithOutput(serverStream);
            Program.ConfigureServer(options, profile);
        });

        _client = await LanguageClient.From(options =>
        {
            options.WithInput(clientStream).WithOutput(clientStream);
            options.WithRootUri(DocumentUri.FromFileSystemPath(workspaceFolder));
            options.WithWorkspaceFolder(DocumentUri.FromFileSystemPath(workspaceFolder), "test-workspace");

            // Advertise refresh support — the server's SemanticTokensRefreshHandler skips the
            // request unless workspace.semanticTokens.refreshSupport is true.
            options.WithCapability(new SemanticTokensWorkspaceCapability { RefreshSupport = true });

            // Sink for the server-initiated refresh request.
            options.OnRequest(WorkspaceNames.SemanticTokensRefresh, (CancellationToken _) =>
            {
                RecordRefresh();
                return Task.CompletedTask;
            });
        }).ConfigureAwait(false);

        _server = await serverTask.ConfigureAwait(false);
    }

    private void RecordRefresh()
    {
        lock (_refreshLock)
        {
            _refreshCount++;
            var prev = _refreshSignal;
            _refreshSignal = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            prev.TrySetResult(_refreshCount);
        }
    }

    /// <summary>
    /// Waits until at least <paramref name="minCount"/> refresh requests have been received,
    /// or the timeout elapses.  Returns true if the threshold was reached.
    /// </summary>
    public async Task<bool> WaitForRefreshAsync(int minCount = 1, int timeoutMs = 5000)
    {
        var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
        while (true)
        {
            Task<int> wait;
            lock (_refreshLock)
            {
                if (_refreshCount >= minCount) return true;
                wait = _refreshSignal.Task;
            }
            var remaining = (int)(deadline - DateTime.UtcNow).TotalMilliseconds;
            if (remaining <= 0) return false;
            var completed = await Task.WhenAny(wait, Task.Delay(remaining)).ConfigureAwait(false);
            if (completed != wait) return RefreshCount >= minCount;
        }
    }

    public ValueTask DisposeAsync()
    {
        try { (_client as IDisposable)?.Dispose(); } catch { }
        try { _server?.Dispose(); } catch { }
        return ValueTask.CompletedTask;
    }
}
