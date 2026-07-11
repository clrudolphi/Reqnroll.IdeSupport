using System.Collections.Generic;

namespace Reqnroll.IdeSupport.Common.Telemetry;

/// <summary>IGuidanceConfiguration</summary>
public interface IGuidanceConfiguration
{
    /// <summary>Gets the guidance step shown right after installation.</summary>
    GuidanceStep Installation { get; }

    /// <summary>Gets the guidance step shown after an upgrade.</summary>
    GuidanceStep Upgrade { get; }

    /// <summary>Gets the ordered sequence of usage-based guidance steps.</summary>
    IEnumerable<GuidanceStep> UsageSequence { get; }
}
