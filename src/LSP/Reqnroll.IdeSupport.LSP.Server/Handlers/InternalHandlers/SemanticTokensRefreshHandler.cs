using System.Threading;
using MediatR;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using Reqnroll.IdeSupport.Common.Diagnostics;
using Reqnroll.IdeSupport.LSP.Server.Notifications;

namespace Reqnroll.IdeSupport.LSP.Server.Handlers.InternalHandlers;

/// <summary>
/// Handles <see cref="GherkinDocumentParsedNotification"/> by asking the LSP client
/// to refresh its semantic tokens. No tag encoding is performed here; encoding is
/// deferred until the client sends a <c>textDocument/semanticTokens/full</c> request.
/// </summary>
/// <remarks>
/// Multiple parse notifications can arrive in quick succession (e.g. when several
/// files open at once). A debounce window collapses those bursts into a single
/// <c>workspace/semanticTokens/refresh</c> request so the client is not flooded.
/// The refresh is also guarded by a client-capability check: if the client did not
/// advertise <c>workspace.semanticTokens.refreshSupport</c> the request is skipped.
/// </remarks>
public class SemanticTokensRefreshHandler : INotificationHandler<GherkinDocumentParsedNotification>
{
    private static readonly TimeSpan DebounceDelay = TimeSpan.FromMilliseconds(500);

    private readonly ILanguageServerFacade _languageServer;
    private readonly IDeveroomLogger _logger;

    private CancellationTokenSource? _debounceCts;
    private readonly object _debounceLock = new object();

    public SemanticTokensRefreshHandler(ILanguageServerFacade languageServer, IDeveroomLogger logger)
    {
        _languageServer = languageServer;
        _logger = logger;
    }

    public Task Handle(GherkinDocumentParsedNotification notification, CancellationToken cancellationToken)
    {
        // Guard: only send the refresh if the client advertised support for it.
        var semanticTokensWorkspace = _languageServer.ClientSettings.Capabilities?.Workspace?.SemanticTokens;
        if (semanticTokensWorkspace is null || !semanticTokensWorkspace.Value.IsSupported ||
            semanticTokensWorkspace.Value.Value?.RefreshSupport != true)
            return Task.CompletedTask;

        _logger.LogVerbose($"GherkinDocumentParsed: scheduling semantic token refresh for {notification.Uri} v{notification.Version}");

        // Debounce: cancel any pending refresh and start a new delayed one.
        CancellationTokenSource newCts;
        lock (_debounceLock)
        {
            _debounceCts?.Cancel();
            _debounceCts?.Dispose();
            newCts = _debounceCts = new CancellationTokenSource();
        }

        _ = SendRefreshAfterDelayAsync(newCts.Token);
        return Task.CompletedTask;
    }

    private async Task SendRefreshAfterDelayAsync(CancellationToken debounceToken)
    {
        try
        {
            await Task.Delay(DebounceDelay, debounceToken).ConfigureAwait(false);

            _logger.LogVerbose("SemanticTokensRefreshHandler: sending workspace/semanticTokens/refresh");
            await _languageServer.Client
                .SendRequest(WorkspaceNames.SemanticTokensRefresh)
                .ReturningVoid(CancellationToken.None)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // A newer notification superseded this one — normal debounce cancellation.
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"SemanticTokens refresh request failed: {ex.Message}");
        }
    }
}
