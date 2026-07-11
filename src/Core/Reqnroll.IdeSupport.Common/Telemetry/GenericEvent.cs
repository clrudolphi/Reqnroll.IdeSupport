using System.Collections.Generic;
using System.Collections.Immutable;
namespace Reqnroll.IdeSupport.Common.Telemetry;

/// <summary>GenericEvent</summary>
public record GenericEvent : ITelemetryEvent
{
    /// <summary>Initializes a new instance of the <see cref="GenericEvent"/> class.</summary>
    public GenericEvent(string eventName, IEnumerable<KeyValuePair<string, object>> properties)
    {
        EventName = eventName;
        Properties = properties.ToImmutableDictionary();
    }

    /// <summary>Initializes a new instance of the <see cref="GenericEvent"/> class.</summary>
    public GenericEvent(string eventName) : this(eventName, ImmutableDictionary<string, object>.Empty)
    {
    }

    /// <summary>Gets the telemetry event name.</summary>
    public string EventName { get; }
    /// <summary>Gets the event's properties.</summary>
    public ImmutableDictionary<string, object> Properties { get; }
}
