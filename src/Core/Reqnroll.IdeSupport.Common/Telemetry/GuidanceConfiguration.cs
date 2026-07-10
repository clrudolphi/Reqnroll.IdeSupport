#nullable disable
using Reqnroll;
using System.Collections.Generic;

namespace Reqnroll.IdeSupport.Common.Telemetry;

public class GuidanceConfiguration : IGuidanceConfiguration
{
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

    public GuidanceStep Installation { get; }

    public GuidanceStep Upgrade { get; }

    public IEnumerable<GuidanceStep> UsageSequence { get; }
}
