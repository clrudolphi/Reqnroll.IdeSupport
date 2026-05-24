using Reqnroll.IdeSupport.Common;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Reqnroll.IdeSupport.Common.ProjectSystem;

// Refactoring note: replacing the use of Microsoft.VisualStudio.Utilities.PropertyCollection with ConcurrentDictionary<type, object>
public interface IProjectScope :  IDisposable 
{
    IIdeScope IdeScope { get; }
    string ProjectName { get; }
    string ProjectFullName { get; }
    string ProjectFolder { get; }

    ConcurrentDictionary<Type, object> Properties { get; }
    IEnumerable<NuGetPackageReference> PackageReferences { get; }
    string OutputAssemblyPath { get; }
    string TargetFrameworkMoniker { get; }
    string TargetFrameworkMonikers { get; }
    string PlatformTargetName { get; }
    string DefaultNamespace { get; }

    //void AddFile(string targetFilePath, string template);
    int? GetFeatureFileCount();
    string[] GetProjectFiles(string extension);
}
