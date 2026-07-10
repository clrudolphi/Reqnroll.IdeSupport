using System.Collections.Generic;

namespace Reqnroll.IdeSupport.Common.Telemetry;

/// <summary>IGuidanceConfiguration</summary>
public interface IGuidanceConfiguration
{
    /// <summary>Gets or sets the installation.</summary>
    GuidanceStep Installation { get; }

    /// <summary>Gets or sets the upgrade.</summary>
    GuidanceStep Upgrade { get; }

    /// <summary>Gets or sets the usage sequence.</summary>
    IEnumerable<GuidanceStep> UsageSequence { get; }
}
