#nullable disable
using Reqnroll.IdeSupport.Common.ProjectSystem.Settings;

namespace Reqnroll.IdeSupport.Common.ProjectSystem.Settings;

/// <summary>ReqnrollSettings</summary>
public class ReqnrollSettings
{
    /// <summary>Initializes a new instance of the <see cref="ReqnrollSettings"/> class.</summary>
    public ReqnrollSettings()
    {
        Traits = ReqnrollProjectTraits.None;
    }

    /// <summary>Initializes a new instance of the <see cref="ReqnrollSettings"/> class.</summary>
    public ReqnrollSettings(NuGetVersion version, ReqnrollProjectTraits traits, string generatorFolder,
        string configFilePath)
    {
        Version = version;
        Traits = traits;
        GeneratorFolder = generatorFolder;
        ConfigFilePath = configFilePath;
    }

    /// <summary>Gets or sets the version.</summary>
    public NuGetVersion Version { get; set; }
    /// <summary>Gets or sets the traits.</summary>
    public ReqnrollProjectTraits Traits { get; set; }
    /// <summary>Gets or sets the generator folder.</summary>
    public string GeneratorFolder { get; set; }
    /// <summary>Gets or sets the config file path.</summary>
    public string ConfigFilePath { get; set; }
}
