#nullable disable

using System.IO;

namespace Reqnroll.IdeSupport.Common.ProjectSystem;

/// <summary>NuGetPackageReference</summary>
public record NuGetPackageReference
{
    /// <summary>Initializes a new instance of the <see cref="NuGetPackageReference"/> class.</summary>
    public NuGetPackageReference(string packageName, NuGetVersion version, string installPath)
    {
        PackageName = packageName;
        Version = version;
        InstallPath = installPath?.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }

    /// <summary>Gets or sets the package name.</summary>
    public string PackageName { get; }
    /// <summary>Gets or sets the version.</summary>
    public NuGetVersion Version { get; }
    /// <summary>Gets or sets the install path.</summary>
    public string InstallPath { get; }

    /// <summary>Gets or sets the to string.</summary>
    public override string ToString() => $"{PackageName}/{Version}";
}
