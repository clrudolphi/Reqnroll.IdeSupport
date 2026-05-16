using Reqnroll.IDE.Common.Configuration;
using System;

namespace Reqnroll.IDE.Common.ProjectSystem.Configuration;

public interface IDeveroomConfigurationProvider
{
    //event EventHandler<EventArgs> WeakConfigurationChanged;
    DeveroomConfiguration GetConfiguration();
}
