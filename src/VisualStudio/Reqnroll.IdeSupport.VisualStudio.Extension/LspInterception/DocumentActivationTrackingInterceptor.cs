using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace Reqnroll.IdeSupport.VisualStudio.Extension.LspInterception;

/// <summary>
/// Drives <see cref="DocumentActivationState"/>'s <c>didOpen</c>/<c>didClose</c> transitions
/// (issue #85) from the VS→Server send pipeline, and — in the one case where a
/// <c>reqnroll/documentActivated</c> send needs to happen in direct response to the very
/// <c>didOpen</c> message being intercepted — sends it itself rather than racing a separate
/// caller against the pump's own forwarding write.
/// </summary>
/// <remarks>
/// <para>
/// The common case (a tab already open when the user switches to it) is handled entirely
/// outside this interceptor: a VS-side <c>WindowActivated</c> listener calls
/// <see cref="DocumentActivationState.OnWindowActivated"/> directly and, on
/// <see cref="DocumentActivationAction.SendNow"/>, sends the notification itself via
/// <see cref="LspInterceptingPipe.SendNotificationToServerAsync"/> — safe because by that point
/// <c>didOpen</c> has already been forwarded (the state is <c>Opened</c>, not
/// <c>ActivationPending</c>), so there is nothing to sequence against.
/// </para>
/// <para>
/// The rarer case this interceptor exists for: <c>WindowActivated</c> fires before
/// <c>didOpen</c> arrives (<see cref="DocumentActivationPhase.ActivationPending"/>). When
/// <c>didOpen</c> finally shows up, <see cref="DocumentActivationState.OnDidOpen"/> returns
/// <see cref="DocumentActivationAction.SendNow"/> — but the server must see <c>didOpen</c>
/// <em>before</em> <c>documentActivated</c> (the handler no-ops if it can't find an open
/// buffer for the URI). Consuming the message here and re-forwarding it manually, followed by
/// the activation notification, both via the same awaited
/// <see cref="LspInterceptingPipe.SendNotificationToServerAsync"/> calls, keeps the two writes
/// in the wire order this interceptor chose rather than leaving it to a race between the pump's
/// own passthrough write and a separately-scheduled injected send.
/// </para>
/// </remarks>
internal sealed class DocumentActivationTrackingInterceptor : ILspMessageInterceptor
{
    private readonly DocumentActivationState                        _state;
    private readonly Func<LspInterceptingPipe?>                      _getPipe;
    private readonly ILogger<DocumentActivationTrackingInterceptor>  _logger;

    /// <summary>Creates the interceptor over the shared activation-state tracker and a deferred pipe accessor.</summary>
    public DocumentActivationTrackingInterceptor(
        DocumentActivationState       state,
        Func<LspInterceptingPipe?>    getPipe,
        ILogger<DocumentActivationTrackingInterceptor> logger)
    {
        _state   = state   ?? throw new ArgumentNullException(nameof(state));
        _getPipe = getPipe ?? throw new ArgumentNullException(nameof(getPipe));
        _logger  = logger  ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<LspInterceptorResult> InterceptAsync(
        LspMessage        message,
        CancellationToken cancellationToken)
    {
        if (message.Method == "textDocument/didClose")
        {
            if (UriToFeatureFilePath(message) is { } closedPath)
            {
                _state.OnDidClose(closedPath);
                _logger.LogTrace(
                    "DocumentActivationTrackingInterceptor: didClose for {FileName} — activation state reset.",
                    Path.GetFileName(closedPath));
            }
            return LspInterceptorResult.PassThrough;
        }

        if (message.Method != "textDocument/didOpen")
            return LspInterceptorResult.PassThrough;

        if (UriToFeatureFilePath(message) is not { } path)
            return LspInterceptorResult.PassThrough;

        var action = _state.OnDidOpen(path);
        if (action != DocumentActivationAction.SendNow)
        {
            _logger.LogTrace(
                "DocumentActivationTrackingInterceptor: didOpen for {FileName} — no pending activation, passthrough.",
                Path.GetFileName(path));
            return LspInterceptorResult.PassThrough;
        }

        var pipe = _getPipe();
        if (pipe is null)
        {
            _logger.LogWarning(
                "DocumentActivationTrackingInterceptor: pipe not available for {FileName}; letting didOpen pass through without an activation notification.",
                Path.GetFileName(path));
            return LspInterceptorResult.PassThrough;
        }

        // Capture all message data before any async call — re-entrancy safe.
        // If a second didOpen for the same or different file calls back into this
        // interceptor while the first's SendNotificationToServerAsync is awaiting,
        // we must not re-read message.Body after yielding the thread.
        var paramsJson = message.Body["params"]?.ToString();

        // Re-forward didOpen ourselves, then send documentActivated — both awaited in order —
        // so the server sees the buffer before it's asked to recompute against it. Consuming
        // here stops the pump's own passthrough write for this message.
        await pipe.SendNotificationToServerAsync("textDocument/didOpen", paramsJson, cancellationToken)
                  .ConfigureAwait(false);

        _logger.LogInformation(
            "DocumentActivationTrackingInterceptor: activation preceded didOpen for {FileName}; sending reqnroll/documentActivated now.",
            Path.GetFileName(path));

        // Compute the activation payload from the already-captured data, not from a second
        // read of message.Body (which may have been recycled if the pump re-entered us).
        var docUri = message.Body["params"]?["textDocument"]?["uri"]?.Value<string>();
        var activatedParamsJson = $"{{\"uri\":{Newtonsoft.Json.JsonConvert.ToString(docUri)}}}";
        await pipe.SendNotificationToServerAsync("reqnroll/documentActivated", activatedParamsJson, cancellationToken)
                  .ConfigureAwait(false);

        return LspInterceptorResult.Consume;
    }

    private static string? UriToFeatureFilePath(LspMessage message)
    {
        var uri = message.Body["params"]?["textDocument"]?["uri"]?.Value<string>();
        if (uri is null || !uri.EndsWith(".feature", StringComparison.OrdinalIgnoreCase))
            return null;
        if (!Uri.TryCreate(uri, UriKind.Absolute, out var parsed) || parsed.Scheme != "file")
            return null;
        return parsed.LocalPath;
    }
}
