namespace Reqnroll.IdeSupport.Common.ProjectSystem.Settings;

/// <summary>ProjectPlatformTarget</summary>
public enum ProjectPlatformTarget
{
    /// <summary>The platform target could not be determined.</summary>
    Unknown,
    /// <summary>The project targets Any CPU.</summary>
    AnyCpu,
    /// <summary>The project targets 32-bit (x86).</summary>
    x86,
    /// <summary>The project targets 64-bit (x64).</summary>
    x64,
    /// <summary>The project targets ARM64.</summary>
    Arm64
}
