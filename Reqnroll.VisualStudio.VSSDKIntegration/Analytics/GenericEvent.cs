#nullable enable
using Reqnroll.IDE.Common.Analytics;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;

namespace Reqnroll.VisualStudio.Analytics;

[Export(typeof(IAnalyticsEvent))]
public record GenericEvent : Reqnroll.IDE.Common.Analytics.GenericEvent
{
    public GenericEvent(string eventName, IEnumerable<KeyValuePair<string, object>> properties) : base(eventName, properties) { }

    public GenericEvent(string eventName) : this(eventName, ImmutableDictionary<string, object>.Empty)
    {
    }
}
