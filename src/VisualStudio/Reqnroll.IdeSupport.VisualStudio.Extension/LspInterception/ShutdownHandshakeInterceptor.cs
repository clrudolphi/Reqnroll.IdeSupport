using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Reqnroll.IdeSupport.VisualStudio.Extension.LspInterception;

/// <summary>
/// Observes the LSP <c>shutdown</c> request/response handshake so
/// <see cref="LspServerConnectionService"/> knows whether it's safe to ask the server to
/// <c>exit</c> gracefully rather than killing the process outright.
/// </summary>
/// <remarks>
/// Registered on both pipelines (same pattern as <see cref="CodeLensRefreshInterceptor"/>): it
/// captures the request id of an outgoing <c>shutdown</c> request (VS→Server) and watches for the
/// matching response (Server→VS). Per the LSP spec, sending <c>exit</c> without a prior successful
/// <c>shutdown</c> is an abnormal-exit signal — this interceptor exists purely to make that
/// precondition observable. It never consumes a message.
/// </remarks>
internal sealed class ShutdownHandshakeInterceptor : ILspMessageInterceptor
{
    private readonly ILogger<ShutdownHandshakeInterceptor> _logger;
    private string? _shutdownRequestId;
    private volatile bool _shutdownObserved;

    /// <summary>Creates the interceptor over the given logger.</summary>
    public ShutdownHandshakeInterceptor(ILogger<ShutdownHandshakeInterceptor> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// <see langword="true"/> once the server has responded to VS's <c>shutdown</c> request —
    /// the only state in which sending a bare <c>exit</c> notification is spec-compliant.
    /// </summary>
    public bool ShutdownObserved => _shutdownObserved;

    /// <inheritdoc />
    public Task<LspInterceptorResult> InterceptAsync(LspMessage message, CancellationToken cancellationToken)
    {
        if (message.Direction == LspMessageDirection.Send && message.IsRequest &&
            string.Equals(message.Method, "shutdown", StringComparison.Ordinal))
        {
            _shutdownRequestId = message.Id?.ToString();
        }
        else if (message.Direction == LspMessageDirection.Receive && message.IsResponse &&
                 _shutdownRequestId is not null &&
                 string.Equals(message.Id?.ToString(), _shutdownRequestId, StringComparison.Ordinal))
        {
            _shutdownObserved = true;
            _logger.LogInformation(
                "ShutdownHandshakeInterceptor: observed shutdown response — exit is now safe to request.");
        }

        return Task.FromResult(LspInterceptorResult.PassThrough);
    }
}
