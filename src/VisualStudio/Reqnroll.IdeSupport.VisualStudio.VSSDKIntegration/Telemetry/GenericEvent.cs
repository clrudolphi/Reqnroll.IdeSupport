using System.Collections.Immutable;
using System.ComponentModel.Composition;
#nullable enable
using Reqnroll.IdeSupport.Common.Telemetry;

namespace Reqnroll.IdeSupport.VisualStudio.Telemetry;

[Export(typeof(ITelemetryEvent))]
public record GenericEvent : Reqnroll.IdeSupport.Common.Telemetry.GenericEvent
{
    public GenericEvent(string eventName, IEnumerable<KeyValuePair<string, object>> properties) : base(eventName, properties) { }

    public GenericEvent(string eventName) : this(eventName, ImmutableDictionary<string, object>.Empty)
    {
    }
}
