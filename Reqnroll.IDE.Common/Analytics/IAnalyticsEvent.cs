#nullable enable
using System.Collections.Immutable;

namespace Reqnroll.IDE.Common.Analytics;

public interface IAnalyticsEvent
{
    string EventName { get; }
    ImmutableDictionary<string, object> Properties { get; }
}
