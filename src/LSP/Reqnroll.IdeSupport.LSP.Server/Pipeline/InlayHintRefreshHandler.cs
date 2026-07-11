using MediatR;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using Reqnroll.IdeSupport.Common.Logging;
using Reqnroll.IdeSupport.LSP.Server.Performance;
using Reqnroll.IdeSupport.LSP.Server.Protocol;
namespace Reqnroll.IdeSupport.LSP.Server.Pipeline;

/// <summary>
/// Handles <see cref="MatchCacheChangedNotification"/> by asking the LSP client to refresh its
/// inlay hints (F23), so binding-info hints stay in sync after edits/build without the user
/// having to scroll the viewport to re-pull them.
/// </summary>
/// <remarks>
/// Mirrors <see cref="SemanticTokensRefreshHandler"/>: debounces bursts of match-cache
/// notifications into a single <c>workspace/inlayHint/refresh</c> request, and only sends it when
/// the client advertised <c>workspace.inlayHint.refreshSupport</c>.
/// </remarks>
public class InlayHintRefreshHandler : INotificationHandler<MatchCacheChangedNotification>
{
    private static readonly TimeSpan DebounceDelay = TimeSpan.FromMilliseconds(500);

    private readonly ILanguageServerFacade _languageServer;
    private readonly IIdeSupportLogger _logger;
    private readonly IOperationDurationRecorder _recorder;

    private CancellationTokenSource? _debounceCts;
    private readonly object _debounceLock = new object();

    /// <summary>Initializes a new instance of the <see cref="InlayHintRefreshHandler"/> class.</summary>
    public InlayHintRefreshHandler(
        ILanguageServerFacade languageServer,
        IIdeSupportLogger logger,
        IOperationDurationRecorder? recorder = null)
    {
        _languageServer = languageServer;
        _logger = logger;
        _recorder = recorder ?? NullOperationDurationRecorder.Instance;
    }

    /// <summary>Handles an internal <see cref="MatchCacheChangedNotification"/> by debouncing and, if the client supports it, sending a <c>workspace/inlayHint/refresh</c> request.</summary>
    public Task Handle(MatchCacheChangedNotification notification, CancellationToken cancellationToken)
    {
        var inlayHintWorkspace = _languageServer.ClientSettings.Capabilities?.Workspace?.InlayHint;
        if (inlayHintWorkspace is null || !inlayHintWorkspace.Value.IsSupported ||
            inlayHintWorkspace.Value.Value?.RefreshSupport != true)
            return Task.CompletedTask;

        _logger.LogVerbose($"MatchCacheChanged: scheduling inlay hint refresh for {notification.Uri} v{notification.Version}");

        CancellationTokenSource newCts;
        lock (_debounceLock)
        {
#pragma warning disable VSTHRD103 // Cancel() inside a lock; CancelAsync() cannot be awaited here
            _debounceCts?.Cancel();
#pragma warning restore VSTHRD103
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

            using var _perf = _recorder.Measure(LspMethodNames.WorkspaceInlayHintRefresh);

            _logger.LogVerbose("InlayHintRefreshHandler: sending workspace/inlayHint/refresh");
            await _languageServer.Client
                .SendRequest(WorkspaceNames.InlayHintRefresh)
                .ReturningVoid(CancellationToken.None)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            _logger.LogVerbose("InlayHintRefreshHandler: debounce cancelled — superseded by newer notification");
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"InlayHint refresh request failed: {ex.Message}");
        }
    }
}
