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

    /// <summary>Gets or sets the user level.</summary>
    public GuidanceNotification UserLevel { get; }

    /// <summary>Gets or sets the usage days.</summary>
    public int? UsageDays { get; }

    /// <summary>Gets or sets the url.</summary>
    public string Url { get; }
}
