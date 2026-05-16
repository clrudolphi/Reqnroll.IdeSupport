using System;
using System.Collections.Generic;
using System.Linq;

namespace Reqnroll.IDE.Common.Analytics;

public interface IGuidanceConfiguration
{
    GuidanceStep Installation { get; }

    GuidanceStep Upgrade { get; }

    IEnumerable<GuidanceStep> UsageSequence { get; }
}
