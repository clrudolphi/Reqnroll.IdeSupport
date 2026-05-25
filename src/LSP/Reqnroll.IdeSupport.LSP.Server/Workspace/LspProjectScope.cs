using Reqnroll.IdeSupport.Common;
using Reqnroll.IdeSupport.Common.ProjectSystem;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;

namespace Reqnroll.IdeSupport.LSP.Server.Workspace;

/// <summary>
/// Minimal <see cref="IProjectScope"/> for one LSP workspace root folder.
/// Configuration is loaded lazily by <c>ConfigurationProjectSystemExtensions.GetDeveroomConfigurationProvider</c>
/// which stores a <see cref="Reqnroll.IdeSupport.Common.ProjectSystem.Configuration.ProjectScopeDeveroomConfigurationProvider"/>
/// in <see cref="Properties"/>.
/// </summary>
public sealed class LspProjectScope : IProjectScope
{
    public LspProjectScope(string rootFolder, IIdeScope ideScope)
    {
        ProjectFolder = Path.GetFullPath(rootFolder);
        ProjectName = Path.GetFileName(ProjectFolder.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        ProjectFullName = ProjectFolder;
        IdeScope = ideScope;
        Properties = new ConcurrentDictionary<Type, object>();
    }

    public IIdeScope IdeScope { get; }
    public string ProjectName { get; }
    public string ProjectFullName { get; }
    public string ProjectFolder { get; }
    public ConcurrentDictionary<Type, object> Properties { get; }

    public IEnumerable<NuGetPackageReference> PackageReferences => ImmutableArray<NuGetPackageReference>.Empty;
    public string OutputAssemblyPath => string.Empty;
    public string TargetFrameworkMoniker => string.Empty;
    public string TargetFrameworkMonikers => string.Empty;
    public string PlatformTargetName => string.Empty;
    public string DefaultNamespace => string.Empty;

    public int? GetFeatureFileCount()
    {
        try
        {
            return IdeScope.FileSystem.Directory
                .GetFiles(ProjectFolder, "*.feature", SearchOption.AllDirectories)
                .Length;
        }
        catch
        {
            return null;
        }
    }

    public string[] GetProjectFiles(string extension)
    {
        try
        {
            var pattern = $"*{extension}";
            return IdeScope.FileSystem.Directory
                .GetFiles(ProjectFolder, pattern, SearchOption.AllDirectories);
        }
        catch
        {
            return Array.Empty<string>();
        }
    }

    public void Dispose()
    {
        foreach (var disposable in Properties.Values.OfType<IDisposable>())
            disposable.Dispose();
    }

    public override string ToString() => ProjectName;
}
