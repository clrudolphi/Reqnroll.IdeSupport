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

    /// <summary>No-op: this null-object implementation holds no resources to release.</summary>
    public void Dispose()
    {
    }

    /// <summary>Gets the owning IDE scope.</summary>
    public IIdeScope IdeScope { get; }
    /// <summary>Gets the project name; always empty for this null-object implementation.</summary>
    public string ProjectName { get; }
    /// <summary>Gets the full project path; always empty for this null-object implementation.</summary>
    public string ProjectFullName { get; }
    /// <summary>Gets the project folder; always empty for this null-object implementation.</summary>
    public string ProjectFolder { get; }
    /// <summary>Gets the project's NuGet package references; always empty for this null-object implementation.</summary>
    public IEnumerable<NuGetPackageReference> PackageReferences { get; }
    /// <summary>Gets the output assembly path; always empty for this null-object implementation.</summary>
    public string OutputAssemblyPath { get; }
    /// <summary>Gets the target framework moniker; always empty for this null-object implementation.</summary>
    public string TargetFrameworkMoniker { get; }
    /// <summary>Gets the target framework monikers; always empty for this null-object implementation.</summary>
    public string TargetFrameworkMonikers { get; }
    /// <summary>Gets the platform target name; always empty for this null-object implementation.</summary>
    public string PlatformTargetName { get; }
    /// <summary>Gets the project's default namespace; always empty for this null-object implementation.</summary>
    public string DefaultNamespace { get; }

    /// <summary>Gets the bag of ambient properties associated with this project scope.</summary>
    public ConcurrentDictionary<Type, object> Properties { get; }

    /// <summary>No-op: this null-object implementation does not add files.</summary>
    public void AddFile(string targetFilePath, string template)
    {
    }

    /// <summary>Always returns zero for this null-object implementation.</summary>
    public int? GetFeatureFileCount() => 0;

    /// <summary>Always returns an empty array for this null-object implementation.</summary>
    public string[] GetProjectFiles(string extension) => Array.Empty<string>();
}
