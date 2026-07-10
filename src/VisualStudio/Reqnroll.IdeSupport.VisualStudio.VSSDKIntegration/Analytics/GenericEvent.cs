#nullable enable
using Reqnroll.IdeSupport.Common.Telemetry;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;

namespace Reqnroll.IdeSupport.VisualStudio.Analytics;

[Export(typeof(ITelemetryEvent))]
public record GenericEvent : Reqnroll.IdeSupport.Common.Telemetry.GenericEvent
{
    public GenericEvent(string eventName, IEnumerable<KeyValuePair<string, object>> properties) : base(eventName, properties) { }

    public GenericEvent(string eventName) : this(eventName, ImmutableDictionary<string, object>.Empty)
    {
    }
}
