using Reqnroll.IdeSupport.Common.ProjectSystem;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Reqnroll.IdeSupport.Common;

/// <summary>VoidProjectScope</summary>
public class VoidProjectScope : IProjectScope
{
    /// <summary>Initializes a new instance of the <see cref="VoidProjectScope"/> class.</summary>
    public VoidProjectScope(IIdeScope ideScope)
    {
        Properties = new ConcurrentDictionary<Type, object>();
        IdeScope = ideScope;
        ProjectName = string.Empty;
        ProjectFullName = string.Empty;
        ProjectFolder = string.Empty;
        PackageReferences = ImmutableArray<NuGetPackageReference>.Empty;
        OutputAssemblyPath = string.Empty;
        TargetFrameworkMoniker = string.Empty;
        TargetFrameworkMonikers = string.Empty;
        PlatformTargetName = string.Empty;
        DefaultNamespace = string.Empty;
    }

    /// <summary>Gets or sets the dispose.</summary>
    public void Dispose()
    {
    }

    /// <summary>Gets or sets the ide scope.</summary>
    public IIdeScope IdeScope { get; }
    /// <summary>Gets or sets the project name.</summary>
    public string ProjectName { get; }
    /// <summary>Gets or sets the project full name.</summary>
    public string ProjectFullName { get; }
    /// <summary>Gets or sets the project folder.</summary>
    public string ProjectFolder { get; }
    /// <summary>Gets or sets the package references.</summary>
    public IEnumerable<NuGetPackageReference> PackageReferences { get; }
    /// <summary>Gets or sets the output assembly path.</summary>
    public string OutputAssemblyPath { get; }
    /// <summary>Gets or sets the target framework moniker.</summary>
    public string TargetFrameworkMoniker { get; }
    /// <summary>Gets or sets the target framework monikers.</summary>
    public string TargetFrameworkMonikers { get; }
    /// <summary>Gets or sets the platform target name.</summary>
    public string PlatformTargetName { get; }
    /// <summary>Gets or sets the default namespace.</summary>
    public string DefaultNamespace { get; }

    /// <summary>Gets or sets the properties.</summary>
    public ConcurrentDictionary<Type, object> Properties { get; }

    /// <summary>Gets or sets the add file.</summary>
    public void AddFile(string targetFilePath, string template)
    {
    }

    /// <summary>Gets or sets the get feature file count.</summary>
    public int? GetFeatureFileCount() => 0;

    /// <summary>Gets or sets the get project files.</summary>
    public string[] GetProjectFiles(string extension) => Array.Empty<string>();
}
