using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Reqnroll.IdeSupport.Common.Diagnostics;

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
    private readonly DocumentActivationState        _state;
    private readonly Func<LspInterceptingPipe?>      _getPipe;
    private readonly TraceSource                     _trace;

    // TraceSource above is kept for call-site consistency with the rest of LspInterception (see
    // ScaffoldTrackingInterceptor), but per issue #84 nothing ever attaches a listener to it in
    // this codebase — TraceInformation/TraceEvent calls on it are silently discarded. Logging that
    // actually needs to be inspectable (this class exists specifically to fix a hard-to-observe
    // race — see #85) goes through this file logger instead, Info-level explicitly since
    // SynchronousFileLogger()'s default (Warning) would drop LogInfo the same way.
    private readonly IDeveroomLogger _fileLogger = new SynchronousFileLogger(level: TraceLevel.Info);

    public DocumentActivationTrackingInterceptor(
        DocumentActivationState       state,
        Func<LspInterceptingPipe?>    getPipe,
        TraceSource                   trace)
    {
        _state   = state   ?? throw new ArgumentNullException(nameof(state));
        _getPipe = getPipe ?? throw new ArgumentNullException(nameof(getPipe));
        _trace   = trace   ?? throw new ArgumentNullException(nameof(trace));
    }

    public async Task<LspInterceptorResult> InterceptAsync(
        LspMessage        message,
        CancellationToken cancellationToken)
    {
        if (message.Method == "textDocument/didClose")
        {
            if (UriToFeatureFilePath(message) is { } closedPath)
            {
                _state.OnDidClose(closedPath);
                _fileLogger.LogVerbose(
                    $"DocumentActivationTrackingInterceptor: didClose for '{Path.GetFileName(closedPath)}' — activation state reset.");
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
            _fileLogger.LogVerbose(
                $"DocumentActivationTrackingInterceptor: didOpen for '{Path.GetFileName(path)}' — no pending activation, passthrough.");
            return LspInterceptorResult.PassThrough;
        }

        var pipe = _getPipe();
        if (pipe is null)
        {
            _fileLogger.LogWarning(
                $"DocumentActivationTrackingInterceptor: pipe not available for '{Path.GetFileName(path)}'; letting didOpen pass through without an activation notification.");
            return LspInterceptorResult.PassThrough;
        }

        // Re-forward didOpen ourselves, then send documentActivated — both awaited in order —
        // so the server sees the buffer before it's asked to recompute against it. Consuming
        // here stops the pump's own passthrough write for this message.
        var paramsJson = message.Body["params"]?.ToString();
        await pipe.SendNotificationToServerAsync("textDocument/didOpen", paramsJson, cancellationToken)
                  .ConfigureAwait(false);

        _fileLogger.LogInfo(
            $"DocumentActivationTrackingInterceptor: activation preceded didOpen for '{Path.GetFileName(path)}'; sending reqnroll/documentActivated now.");

        var activatedParamsJson = $"{{\"uri\":{Newtonsoft.Json.JsonConvert.ToString(message.Body["params"]?["textDocument"]?["uri"]?.Value<string>())}}}";
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
