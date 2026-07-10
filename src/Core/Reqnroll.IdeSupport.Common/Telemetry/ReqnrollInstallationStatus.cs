using System;

namespace Reqnroll.IdeSupport.Common.Telemetry;

/// <summary>ReqnrollInstallationStatus</summary>
public class ReqnrollInstallationStatus
{
    /// <summary>Gets or sets the magic date.</summary>
    public static readonly DateTime MagicDate = new(2024, 1, 12); // when Reqnroll has born
    /// <summary>Gets or sets the unknown version.</summary>
    public static readonly Version UnknownVersion = new(0, 0);
    /// <summary>Gets or sets the is installed.</summary>
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
