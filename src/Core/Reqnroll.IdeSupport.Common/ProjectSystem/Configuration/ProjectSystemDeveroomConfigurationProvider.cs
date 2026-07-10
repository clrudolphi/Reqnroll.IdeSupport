using Reqnroll.IdeSupport.Common;
using Reqnroll.IdeSupport.Common.Configuration;
using System;

namespace Reqnroll.IdeSupport.Common.ProjectSystem.Configuration;

/// <summary>ProjectSystemDeveroomConfigurationProvider</summary>
public class ProjectSystemDeveroomConfigurationProvider : IDeveroomConfigurationProvider
{
    private readonly DeveroomConfiguration _configuration;

    /// <summary>Initializes a new instance of the <see cref="ProjectSystemDeveroomConfigurationProvider"/> class.</summary>
    public ProjectSystemDeveroomConfigurationProvider(IIdeScope ideScope)
    {
        _configuration = new DeveroomConfiguration(); //TODO: Load solution-level config
    }

    /// <summary>Gets or sets the configuration changed.</summary>
    public event EventHandler ConfigurationChanged;

    /// <summary>Gets or sets the get configuration.</summary>
    public DeveroomConfiguration GetConfiguration() => _configuration;
}
