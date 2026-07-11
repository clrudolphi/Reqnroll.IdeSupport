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

    /// <summary>Raised on any thread when configuration changes. Never raised by this solution-level stub implementation.</summary>
    public event EventHandler ConfigurationChanged;

    /// <summary>Returns the solution-level configuration (currently a fresh default; solution-level loading is not yet implemented).</summary>
    public DeveroomConfiguration GetConfiguration() => _configuration;
}
