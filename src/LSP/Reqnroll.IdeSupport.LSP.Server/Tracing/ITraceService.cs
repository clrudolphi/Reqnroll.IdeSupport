#nullable enable

using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Reqnroll.IdeSupport.LSP.Server.Tracing;

/// <summary>
/// Tracks the LSP <c>trace</c> level negotiated with the client (F41) and issues
/// <c>$/logTrace</c> notifications when it is anything other than <see cref="InitializeTrace.Off"/>.
/// </summary>
public interface ITraceService
{
    /// <summary>
    /// The current trace level: the value from <c>InitializeParams.Trace</c> at startup,
    /// subsequently overridden by any <c>$/setTrace</c> notification.
    /// </summary>
    InitializeTrace Level { get; set; }

    /// <summary>
    /// Sends a <c>$/logTrace</c> notification to the client, unless <see cref="Level"/> is
    /// <see cref="InitializeTrace.Off"/>. <paramref name="verboseMessage"/> is only invoked (and
    /// only sent) when <see cref="Level"/> is <see cref="InitializeTrace.Verbose"/>, so callers
    /// can defer building an expensive detail string until it is actually wanted.
    /// </summary>
    void Trace(string message, System.Func<string>? verboseMessage = null);
}
