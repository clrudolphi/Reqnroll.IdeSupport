#nullable enable
using Reqnroll;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Reqnroll.IdeSupport.Common.Telemetry;

public record GenericEvent : ITelemetryEvent
{
    public GenericEvent(string eventName, IEnumerable<KeyValuePair<string, object>> properties)
    {
        EventName = eventName;
        Properties = properties.ToImmutableDictionary();
    }

    public GenericEvent(string eventName) : this(eventName, ImmutableDictionary<string, object>.Empty)
    {
    }

    public string EventName { get; }
    public ImmutableDictionary<string, object> Properties { get; }
}
