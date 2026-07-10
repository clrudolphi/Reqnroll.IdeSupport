using System.Collections.Generic;

namespace Reqnroll.IdeSupport.Common.Telemetry;

public interface IGuidanceConfiguration
{
    GuidanceStep Installation { get; }

    GuidanceStep Upgrade { get; }

    IEnumerable<GuidanceStep> UsageSequence { get; }
}
