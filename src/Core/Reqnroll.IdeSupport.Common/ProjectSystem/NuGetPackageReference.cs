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

    /// <summary>Gets the NuGet package name.</summary>
    public string PackageName { get; }
    /// <summary>Gets the resolved package version.</summary>
    public NuGetVersion Version { get; }
    /// <summary>Gets the path the package was installed to, with any trailing directory separator trimmed.</summary>
    public string InstallPath { get; }

    /// <summary>Returns the package name and version in the form <c>{PackageName}/{Version}</c>.</summary>
    public override string ToString() => $"{PackageName}/{Version}";
}
