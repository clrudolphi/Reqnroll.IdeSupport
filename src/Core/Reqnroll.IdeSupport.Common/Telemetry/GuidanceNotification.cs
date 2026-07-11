namespace Reqnroll.IdeSupport.Common.Telemetry;

/// <summary>GuidanceNotification</summary>
public enum GuidanceNotification
{
    /// <summary>Notification shown right after the extension is installed.</summary>
    AfterInstall = 1,
    /// <summary>Notification shown after the extension is upgraded to a new version.</summary>
    Upgrade = 2,
    /// <summary>Notification shown after two days of usage.</summary>
    TwoDayUsage = 10,
    /// <summary>Notification shown after five days of usage.</summary>
    FiveDayUsage = 50,
    /// <summary>Notification shown after ten days of usage.</summary>
    TenDayUsage = 75,
    /// <summary>Notification shown after twenty days of usage.</summary>
    TwentyDayUsage = 100,
    /// <summary>Notification shown after a hundred days of usage.</summary>
    HundredDayUsage = 200,
    /// <summary>Notification shown after two hundred days of usage.</summary>
    TwoHundredDayUsage = 300
}
