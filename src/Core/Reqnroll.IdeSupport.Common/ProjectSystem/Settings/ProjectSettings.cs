namespace Reqnroll.IdeSupport.Common.ProjectSystem.Settings;

/// <summary>Initializes a new instance of the <see cref="ProjectSettings"/> class.</summary>
/// <summary>ProjectSettings</summary>
public record ProjectSettings(
    /// <summary>Gets or sets the kind.</summary>
    DeveroomProjectKind Kind,
    /// <summary>Gets or sets the target framework moniker.</summary>
    TargetFrameworkMoniker TargetFrameworkMoniker,
    /// <summary>Gets or sets the target framework monikers.</summary>
    string TargetFrameworkMonikers,
    /// <summary>Gets or sets the platform target.</summary>
    ProjectPlatformTarget PlatformTarget,
    /// <summary>Gets or sets the output assembly path.</summary>
    string OutputAssemblyPath,
    /// <summary>Gets or sets the default namespace.</summary>
    string DefaultNamespace,
    /// <summary>Gets or sets the reqnroll version.</summary>
    NuGetVersion ReqnrollVersion,
    /// <summary>Gets or sets the reqnroll generator folder.</summary>
    string ReqnrollGeneratorFolder,
    /// <summary>Gets or sets the reqnroll config file path.</summary>
    string ReqnrollConfigFilePath,
    /// <summary>Gets or sets the reqnroll project traits.</summary>
    ReqnrollProjectTraits ReqnrollProjectTraits,
    /// <summary>Gets or sets the programming language.</summary>
    ProjectProgrammingLanguage ProgrammingLanguage
)
{
    /// <summary>Gets or sets the is uninitialized.</summary>
    public bool IsUninitialized => Kind == DeveroomProjectKind.Uninitialized;
    /// <summary>Gets or sets the is reqnroll test project.</summary>
    public bool IsReqnrollTestProject => Kind == DeveroomProjectKind.ReqnrollTestProject;
    /// <summary>Gets or sets the is reqnroll lib project.</summary>
    public bool IsReqnrollLibProject => Kind == DeveroomProjectKind.ReqnrollLibProject;
    /// <summary>Gets or sets the is reqnroll project.</summary>
    public bool IsReqnrollProject => IsReqnrollTestProject || IsReqnrollLibProject;
    /// <summary>Gets or sets the is spec flow project.</summary>
    public bool IsSpecFlowProject => IsReqnrollProject && ReqnrollProjectTraits.HasFlag(ReqnrollProjectTraits.LegacySpecFlow);

    /// <summary>Gets or sets the design time feature file generation enabled.</summary>
    public bool DesignTimeFeatureFileGenerationEnabled =>
        ReqnrollProjectTraits.HasFlag(ReqnrollProjectTraits.DesignTimeFeatureFileGeneration);

    /// <summary>Gets or sets the has design time generation replacement.</summary>
    public bool HasDesignTimeGenerationReplacement =>
        ReqnrollProjectTraits.HasFlag(ReqnrollProjectTraits.MsBuildGeneration) ||
        ReqnrollProjectTraits.HasFlag(ReqnrollProjectTraits.XUnitAdapter);

    /// <summary>Gets or sets the get reqnroll version label.</summary>
    public string GetReqnrollVersionLabel() => ReqnrollVersion?.ToString() ?? "n/a";

    /// <summary>Gets or sets the get short label.</summary>
    public string GetShortLabel()
    {
        var result = $"{TargetFrameworkMoniker}";
        result += IsSpecFlowProject
            ? $",SpecFlow:{GetReqnrollVersionLabel()}"
            : $",Reqnroll:{GetReqnrollVersionLabel()}";
        if (PlatformTarget != ProjectPlatformTarget.Unknown && PlatformTarget != ProjectPlatformTarget.AnyCpu)
            result += "," + PlatformTarget;
        if (DesignTimeFeatureFileGenerationEnabled)
            result += ",Gen";
        return result;
    }
}
