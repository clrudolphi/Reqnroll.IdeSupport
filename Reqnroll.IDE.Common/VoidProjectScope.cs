using Reqnroll.IDE.Common.ProjectSystem;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Reqnroll.IDE.Common;

public class VoidProjectScope : IProjectScope
{
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

    public void Dispose()
    {
    }

    public IIdeScope IdeScope { get; }
    public string ProjectName { get; }
    public string ProjectFullName { get; }
    public string ProjectFolder { get; }
    public IEnumerable<NuGetPackageReference> PackageReferences { get; }
    public string OutputAssemblyPath { get; }
    public string TargetFrameworkMoniker { get; }
    public string TargetFrameworkMonikers { get; }
    public string PlatformTargetName { get; }
    public string DefaultNamespace { get; }

    public ConcurrentDictionary<Type, object> Properties { get; }

    public void AddFile(string targetFilePath, string template)
    {
    }

    public int? GetFeatureFileCount() => 0;

    public string[] GetProjectFiles(string extension) => Array.Empty<string>();
}
