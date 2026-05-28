using System.Threading;
using System.Threading.Tasks;

namespace Reqnroll.IdeSupport.VisualStudio.Extension.LspInterception;

/// <summary>
/// Participates in the LSP message interception pipeline for one direction of traffic
/// (VS→Server or Server→VS).
/// </summary>
/// <remarks>
/// <para>
/// Interceptors are invoked in registration order.  The first interceptor that returns
/// <see cref="LspInterceptorResult.Consume"/> stops the chain; the message is not forwarded
/// to its destination (or to subsequent interceptors).  Consuming interceptors are responsible
/// for any required side-effects or dispatch to VS extension services.
/// </para>
/// <para>
/// Interceptors that return <see cref="LspInterceptorResult.PassThrough"/> allow the message
/// to continue to the next interceptor and, ultimately, to VS or the server.
/// </para>
/// </remarks>
internal interface ILspMessageInterceptor
{
    /// <summary>
    /// Inspect (and optionally consume) an LSP message.
    /// </summary>
    /// <param name="message">The parsed LSP message.</param>
    /// <param name="cancellationToken">Cancelled when the connection is torn down.</param>
    /// <returns>
    /// <see cref="LspInterceptorResult.PassThrough"/> to let the message continue,
    /// or <see cref="LspInterceptorResult.Consume"/> to stop further propagation.
    /// </returns>
    Task<LspInterceptorResult> InterceptAsync(LspMessage message, CancellationToken cancellationToken);
}
