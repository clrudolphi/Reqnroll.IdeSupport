#nullable enable

using System.Collections.Generic;

namespace Reqnroll.IdeSupport.LSP.Server.Services;

/// <summary>
/// Allows LSP server components to emit telemetry/event notifications to the client
/// without a direct dependency on OmniSharp's <c>ILanguageServerFacade</c>.
/// <para>
/// The client-side <c>telemetry/event</c> handler (a VS interceptor) receives these
/// notifications and forwards them to <see cref="Common.Analytics.IAnalyticsTransmitter"/>.
/// </para>
/// </summary>
public interface ILspTelemetryService
{
    /// <summary>
    /// Sends a <c>telemetry/event</c> notification to the client with the specified
    /// <paramref name="eventName"/> and <paramref name="properties"/>.
    /// </summary>
    void SendEvent(string eventName, Dictionary<string, object?> properties);
}
