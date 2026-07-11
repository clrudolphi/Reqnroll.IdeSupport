using System.Collections.Generic;

namespace Reqnroll.IdeSupport.Common.Telemetry;

/// <summary>GuidanceConfiguration</summary>
public class GuidanceConfiguration : IGuidanceConfiguration
{
    /// <summary>Initializes a new instance of the <see cref="GuidanceConfiguration"/> class.</summary>
    public GuidanceConfiguration()
    {
        // currently not used
        Installation = new GuidanceStep(GuidanceNotification.AfterInstall, null,
            "https://reqnroll.net/welcome-to-reqnroll/");

        // currently not used
        Upgrade = new GuidanceStep(GuidanceNotification.Upgrade, null, null);

        UsageSequence = new[]
        {
            new GuidanceStep(GuidanceNotification.TwoDayUsage, 2, null),
            new GuidanceStep(GuidanceNotification.FiveDayUsage, 5, null),
            new GuidanceStep(GuidanceNotification.TenDayUsage, 10, null),
            new GuidanceStep(GuidanceNotification.TwentyDayUsage, 20, null),
            new GuidanceStep(GuidanceNotification.HundredDayUsage, 100, null),
            new GuidanceStep(GuidanceNotification.TwoHundredDayUsage, 200, null)
        };
    }

    /// <summary>Gets the guidance step shown right after installation.</summary>
    public GuidanceStep Installation { get; }

    /// <summary>Gets the guidance step shown after an upgrade.</summary>
    public GuidanceStep Upgrade { get; }

    /// <summary>Gets the ordered sequence of usage-based guidance steps.</summary>
    public IEnumerable<GuidanceStep> UsageSequence { get; }
}
