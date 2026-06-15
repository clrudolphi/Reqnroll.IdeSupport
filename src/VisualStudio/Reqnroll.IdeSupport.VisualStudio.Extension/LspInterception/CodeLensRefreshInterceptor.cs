#nullable enable

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Reqnroll.IdeSupport.VisualStudio.Extension.StepCodeLens;

namespace Reqnroll.IdeSupport.VisualStudio.Extension.LspInterception;

/// <summary>
/// Watches for <c>textDocument/didChange</c> notifications sent to the server for
/// <c>.cs</c> files.  After a debounced delay (to let the server process the edit and
/// update the binding registry), invalidates any tracked <see cref="StepCodeLens"/>
/// instances for that file so VS re-calls <c>GetLabelAsync</c> with fresh data.
/// </summary>
internal sealed class CodeLensRefreshInterceptor : ILspMessageInterceptor
{
    private readonly StepCodeLensState _state;
    private readonly TraceSource       _traceSource;

    // Debounce: don't invalidate more than once per 500ms for the same file.
    private static readonly TimeSpan DebounceInterval = TimeSpan.FromMilliseconds(500);
    private string? _lastFileUri;
    private DateTime _lastInvalidation = DateTime.MinValue;

    public CodeLensRefreshInterceptor(StepCodeLensState state, TraceSource traceSource)
    {
        _state       = state;
        _traceSource = traceSource;
    }

    public Task<LspInterceptorResult> InterceptAsync(
        LspMessage message,
        CancellationToken cancellationToken)
    {
        if (message.Direction != LspMessageDirection.Send)
            return Task.FromResult(LspInterceptorResult.PassThrough);

        var body = message.Body;
        if (body is null)
            return Task.FromResult(LspInterceptorResult.PassThrough);

        var methodToken = body["method"];
        if (methodToken is null)
            return Task.FromResult(LspInterceptorResult.PassThrough);
        var method = methodToken.Value<string>();
        if (!string.Equals(method, "textDocument/didChange", StringComparison.Ordinal))
            return Task.FromResult(LspInterceptorResult.PassThrough);

        // Extract the URI from the params
        var uri = body["params"]?["textDocument"]?["uri"]?.Value<string>();
        if (string.IsNullOrEmpty(uri))
            return Task.FromResult(LspInterceptorResult.PassThrough);

        // Only care about .cs files
        if (!uri!.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            return Task.FromResult(LspInterceptorResult.PassThrough);

        // Debounce: skip if we just invalidated this file
        var now = DateTime.UtcNow;
        if (string.Equals(uri, _lastFileUri, StringComparison.OrdinalIgnoreCase) &&
            (now - _lastInvalidation) < DebounceInterval)
            return Task.FromResult(LspInterceptorResult.PassThrough);

        _lastFileUri       = uri;
        _lastInvalidation  = now;

        // Invalidate code lenses for this file.  The interceptor runs on the
        // send-pump thread; the Invalidate() call is safe from any thread and
        // VS will re-call GetLabelAsync on its own paint cycle.
        _state.InvalidateLensesForFile(uri);
        _traceSource.TraceInformation(
            "CodeLensRefreshInterceptor: invalidated lenses for '{0}'", uri);

        return Task.FromResult(LspInterceptorResult.PassThrough);
    }
}
