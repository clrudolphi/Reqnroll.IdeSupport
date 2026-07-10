#nullable enable

using System.Collections.Generic;
using Reqnroll.IdeSupport.Common.Logging;

namespace Reqnroll.IdeSupport.LSP.Server.Telemetry;

/// <summary>
/// Decorates <see cref="ILspTelemetryService"/> to mirror every emitted event to a local JSONL
/// debug log (<see cref="ITelemetryDebugLog"/>) before forwarding to the wrapped service.
/// <para>
/// The mirror is independent of the host transmission and the analytics opt-out, so it records
/// exactly what the server <em>produced</em> even when the event is dropped downstream or
/// telemetry is disabled. When the debug log is not configured the sink is a no-op and this
/// decorator simply forwards.
/// </para>
/// </summary>
public sealed class FileLoggingLspTelemetryService : ILspTelemetryService
{
    private readonly ILspTelemetryService _inner;
    private readonly ITelemetryDebugLog _debugLog;

    public FileLoggingLspTelemetryService(ILspTelemetryService inner, ITelemetryDebugLog debugLog)
    {
        _inner = inner;
        _debugLog = debugLog;
    }

    public void SendEvent(string eventName, Dictionary<string, object?> properties)
    {
        _debugLog.Record("server", eventName, properties);
        _inner.SendEvent(eventName, properties);
    }
}
