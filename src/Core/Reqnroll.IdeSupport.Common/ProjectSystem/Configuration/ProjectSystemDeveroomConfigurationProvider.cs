using Reqnroll.IdeSupport.Common;
using Reqnroll.IdeSupport.Common.Configuration;
using System;

namespace Reqnroll.IdeSupport.Common.ProjectSystem.Configuration;

public class ProjectSystemDeveroomConfigurationProvider : IDeveroomConfigurationProvider
{
    private readonly DeveroomConfiguration _configuration;

    public ProjectSystemDeveroomConfigurationProvider(IIdeScope ideScope)
    {
        _configuration = new DeveroomConfiguration(); //TODO: Load solution-level config
    }

    public event EventHandler ConfigurationChanged;

    public DeveroomConfiguration GetConfiguration() => _configuration;
}
