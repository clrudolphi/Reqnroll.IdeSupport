#nullable enable
using System.Collections.Immutable;

namespace Reqnroll.IdeSupport.Common.Telemetry;

public interface ITelemetryEvent
{
    string EventName { get; }
    ImmutableDictionary<string, object> Properties { get; }
}
