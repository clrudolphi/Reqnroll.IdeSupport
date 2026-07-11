namespace Reqnroll.IdeSupport.Common.ProjectSystem.Settings;

/// <summary>DeveroomProjectKind</summary>
public enum DeveroomProjectKind
{
    /// <summary>The project kind could not be determined.</summary>
    Unknown,
    /// <summary>The project kind has not been evaluated yet.</summary>
    Uninitialized,
    /// <summary>A project that references Reqnroll and contains test/feature files.</summary>
    ReqnrollTestProject,
    /// <summary>A project that references Reqnroll but is a library (not a test project) providing step definitions/bindings.</summary>
    ReqnrollLibProject,
    /// <summary>A project that contains feature files but does not itself reference Reqnroll.</summary>
    FeatureFileContainerProject,
    /// <summary>A project unrelated to Reqnroll.</summary>
    OtherProject
}
