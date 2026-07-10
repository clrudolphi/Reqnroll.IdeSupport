#nullable enable
using System.Collections.Immutable;

namespace Reqnroll.IdeSupport.Common.Telemetry;

/// <summary>ITelemetryEvent</summary>
public interface ITelemetryEvent
{
    /// <summary>Gets or sets the event name.</summary>
    string EventName { get; }
    /// <summary>Gets or sets the properties.</summary>
    ImmutableDictionary<string, object> Properties { get; }
}
