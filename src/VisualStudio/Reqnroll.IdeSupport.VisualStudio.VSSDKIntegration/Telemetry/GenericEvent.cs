using System.Collections.Immutable;
using System.ComponentModel.Composition;
#nullable enable
using Reqnroll.IdeSupport.Common.Telemetry;

namespace Reqnroll.IdeSupport.VisualStudio.Telemetry;

/// <summary>
/// Visual Studio's MEF-exported <see cref="ITelemetryEvent"/> implementation, mirroring the common
/// <see cref="Reqnroll.IdeSupport.Common.Telemetry.GenericEvent"/> base.
/// </summary>
[Export(typeof(ITelemetryEvent))]
public record GenericEvent : Reqnroll.IdeSupport.Common.Telemetry.GenericEvent
{
    /// <summary>Creates an event with the given name and properties.</summary>
    public GenericEvent(string eventName, IEnumerable<KeyValuePair<string, object>> properties) : base(eventName, properties) { }

    /// <summary>Creates an event with the given name and no properties.</summary>
    public GenericEvent(string eventName) : this(eventName, ImmutableDictionary<string, object>.Empty)
    {
    }
}
