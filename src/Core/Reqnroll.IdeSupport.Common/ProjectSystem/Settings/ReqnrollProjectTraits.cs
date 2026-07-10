using System;

namespace Reqnroll.IdeSupport.Common.ProjectSystem.Settings;

[Flags]
/// <summary>ReqnrollProjectTraits</summary>
public enum ReqnrollProjectTraits
{
    /// <summary>Gets or sets the none.</summary>
    None = 0,
    /// <summary>Gets or sets the ms build generation.</summary>
    MsBuildGeneration               = 0b00000001,
    /// <summary>Gets or sets the xunit adapter.</summary>
    XUnitAdapter                    = 0b00000010,
    /// <summary>Gets or sets the design time feature file generation.</summary>
    DesignTimeFeatureFileGeneration = 0b00000100,
    /// <summary>Gets or sets the cucumber expression.</summary>
    CucumberExpression              = 0b00001000,
    /// <summary>Gets or sets the legacy spec flow.</summary>
    LegacySpecFlow                  = 0b00010000,
    /// <summary>Gets or sets the spec flow compatibility.</summary>
    SpecFlowCompatibility           = 0b00100000
}
