namespace Reqnroll.IdeSupport.VisualStudio.Extension.LspInterception;

/// <summary>
/// The result an <see cref="ILspMessageInterceptor"/> returns to control pipeline flow.
/// </summary>
internal enum LspInterceptorResult
{
    /// <summary>
    /// The message should continue to the next interceptor in the pipeline and,
    /// if no subsequent interceptor consumes it, be forwarded to its destination.
    /// </summary>
    PassThrough,

    /// <summary>
    /// The message has been handled by this interceptor and must not be forwarded further.
    /// The interceptor is responsible for any side-effects or VS extension dispatch.
    /// </summary>
    Consume,
}
