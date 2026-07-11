#nullable enable
using System.Collections.Immutable;

namespace Reqnroll.IdeSupport.Common.Telemetry;

/// <summary>ITelemetryEvent</summary>
public interface ITelemetryEvent
{
    /// <summary>Gets the telemetry event name.</summary>
    string EventName { get; }
    /// <summary>Gets the event's properties.</summary>
    ImmutableDictionary<string, object> Properties { get; }
}
