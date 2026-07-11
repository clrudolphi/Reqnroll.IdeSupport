namespace Reqnroll.IdeSupport.Common.Telemetry;

/// <summary>GuidanceStep</summary>
public class GuidanceStep
{
    /// <summary>Initializes a new instance of the <see cref="GuidanceStep"/> class.</summary>
    public GuidanceStep(GuidanceNotification userLevel, int? usageDays, string url)
    {
        UserLevel = userLevel;
        UsageDays = usageDays;
        Url = url;
    }

    /// <summary>Gets the guidance notification this step corresponds to.</summary>
    public GuidanceNotification UserLevel { get; }

    /// <summary>Gets the number of usage days that triggers this step, or <c>null</c> if not usage-based.</summary>
    public int? UsageDays { get; }

    /// <summary>Gets the URL to show for this guidance step, or <c>null</c> if none.</summary>
    public string Url { get; }
}
