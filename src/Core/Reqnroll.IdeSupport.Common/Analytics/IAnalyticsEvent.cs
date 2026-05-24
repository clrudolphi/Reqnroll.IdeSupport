#nullable enable
using System.Collections.Immutable;

namespace Reqnroll.IdeSupport.Common.Analytics;

public interface IAnalyticsEvent
{
    string EventName { get; }
    ImmutableDictionary<string, object> Properties { get; }
}
