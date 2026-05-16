using Reqnroll.IDE.Common;
using Reqnroll.IDE.Common.Configuration;
using System;

namespace Reqnroll.IDE.Common.ProjectSystem.Configuration;

public class ProjectSystemDeveroomConfigurationProvider : IDeveroomConfigurationProvider
{
    private readonly DeveroomConfiguration _configuration;

    public ProjectSystemDeveroomConfigurationProvider(IIdeScope ideScope)
    {
        _configuration = new DeveroomConfiguration(); //TODO: Load solution-level config
    }

    //public event EventHandler<EventArgs> WeakConfigurationChanged
    //{
    //    add { }
    //    remove { }
    //}

    public DeveroomConfiguration GetConfiguration() => _configuration;
}
