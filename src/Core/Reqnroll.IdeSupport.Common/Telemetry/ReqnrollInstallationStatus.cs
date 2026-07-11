using System;

namespace Reqnroll.IdeSupport.Common.Telemetry;

/// <summary>ReqnrollInstallationStatus</summary>
public class ReqnrollInstallationStatus
{
    /// <summary>The date Reqnroll was born (2024-01-12), used as a reference point for install-date calculations.</summary>
    public static readonly DateTime MagicDate = new(2024, 1, 12); // when Reqnroll has born
    /// <summary>Sentinel version indicating no installed version could be detected.</summary>
    public static readonly Version UnknownVersion = new(0, 0);
    /// <summary>Gets whether Reqnroll is installed (i.e. a version other than <see cref="UnknownVersion"/> was detected).</summary>
    public bool IsInstalled => InstalledVersion != UnknownVersion;
    /// <summary>Gets or sets the installed version.</summary>
    public Version InstalledVersion { get; set; } = UnknownVersion;
    /// <summary>Gets or sets the install date.</summary>
    public DateTime InstallDate { get; set; }
    /// <summary>Gets or sets the last used date.</summary>
    public DateTime LastUsedDate { get; set; }
    /// <summary>Gets or sets the usage days.</summary>
    public int UsageDays { get; set; }
    /// <summary>Gets or sets the user level.</summary>
    public int UserLevel { get; set; }
}
