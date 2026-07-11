using Reqnroll.IdeSupport.Common;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Reqnroll.IdeSupport.Common.ProjectSystem;

// Refactoring note: replacing the use of Microsoft.VisualStudio.Utilities.PropertyCollection with ConcurrentDictionary<type, object>
/// <summary>IProjectScope</summary>
public interface IProjectScope :  IDisposable 
{
    /// <summary>Gets the owning IDE scope.</summary>
    IIdeScope IdeScope { get; }
    /// <summary>Gets the project name.</summary>
    string ProjectName { get; }
    /// <summary>Gets the full path of the project file.</summary>
    string ProjectFullName { get; }
    /// <summary>Gets the folder containing the project.</summary>
    string ProjectFolder { get; }

    /// <summary>Gets the bag of ambient properties associated with this project scope.</summary>
    ConcurrentDictionary<Type, object> Properties { get; }
    /// <summary>Gets the project's NuGet package references.</summary>
    IEnumerable<NuGetPackageReference> PackageReferences { get; }
    /// <summary>Gets the path of the project's output assembly.</summary>
    string OutputAssemblyPath { get; }
    /// <summary>Gets the project's target framework moniker.</summary>
    string TargetFrameworkMoniker { get; }
    /// <summary>Gets the project's target framework monikers (for multi-targeting projects).</summary>
    string TargetFrameworkMonikers { get; }
    /// <summary>Gets the project's platform target name (e.g. AnyCPU, x86, x64).</summary>
    string PlatformTargetName { get; }
    /// <summary>Gets the project's default namespace.</summary>
    string DefaultNamespace { get; }

    //void AddFile(string targetFilePath, string template);
    /// <summary>Returns the number of feature files in the project, or <c>null</c> if unknown.</summary>
    int? GetFeatureFileCount();
    /// <summary>Returns the full paths of all project files with the given extension.</summary>
    string[] GetProjectFiles(string extension);
}
