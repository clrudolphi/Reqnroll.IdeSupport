using System;

namespace Reqnroll.IdeSupport.Common.ProjectSystem.Settings;

/// <summary>Flags that describe additional traits of a Reqnroll project.</summary>
[Flags]
public enum ReqnrollProjectTraits
{
    /// <summary>No special traits.</summary>
    None = 0,
    /// <summary>The project uses MSBuild generation.</summary>
    MsBuildGeneration               = 0b00000001,
    /// <summary>The project has an xUnit adapter.</summary>
    XUnitAdapter                    = 0b00000010,
    /// <summary>The project supports design-time feature file generation.</summary>
    DesignTimeFeatureFileGeneration = 0b00000100,
    /// <summary>The project uses Cucumber expressions.</summary>
    CucumberExpression              = 0b00001000,
    /// <summary>The project targets a legacy SpecFlow version.</summary>
    LegacySpecFlow                  = 0b00010000,
    /// <summary>The project is in SpecFlow compatibility mode.</summary>
    SpecFlowCompatibility           = 0b00100000
}