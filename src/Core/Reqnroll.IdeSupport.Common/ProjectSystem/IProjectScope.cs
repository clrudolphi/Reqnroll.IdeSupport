using Reqnroll.IdeSupport.Common;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Reqnroll.IdeSupport.Common.ProjectSystem;

// Refactoring note: replacing the use of Microsoft.VisualStudio.Utilities.PropertyCollection with ConcurrentDictionary<type, object>
/// <summary>IProjectScope</summary>
public interface IProjectScope :  IDisposable 
{
    /// <summary>Gets or sets the ide scope.</summary>
    IIdeScope IdeScope { get; }
    /// <summary>Gets or sets the project name.</summary>
    string ProjectName { get; }
    /// <summary>Gets or sets the project full name.</summary>
    string ProjectFullName { get; }
    /// <summary>Gets or sets the project folder.</summary>
    string ProjectFolder { get; }

    /// <summary>Gets or sets the properties.</summary>
    ConcurrentDictionary<Type, object> Properties { get; }
    /// <summary>Gets or sets the package references.</summary>
    IEnumerable<NuGetPackageReference> PackageReferences { get; }
    /// <summary>Gets or sets the output assembly path.</summary>
    string OutputAssemblyPath { get; }
    /// <summary>Gets or sets the target framework moniker.</summary>
    string TargetFrameworkMoniker { get; }
    /// <summary>Gets or sets the target framework monikers.</summary>
    string TargetFrameworkMonikers { get; }
    /// <summary>Gets or sets the platform target name.</summary>
    string PlatformTargetName { get; }
    /// <summary>Gets or sets the default namespace.</summary>
    string DefaultNamespace { get; }

    //void AddFile(string targetFilePath, string template);
    /// <summary>Gets or sets the get feature file count.</summary>
    int? GetFeatureFileCount();
    /// <summary>Gets or sets the get project files.</summary>
    string[] GetProjectFiles(string extension);
}
