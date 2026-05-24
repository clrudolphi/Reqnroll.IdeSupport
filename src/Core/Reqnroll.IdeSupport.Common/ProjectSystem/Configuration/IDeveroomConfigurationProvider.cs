using Reqnroll.IdeSupport.Common.Configuration;
using System;

namespace Reqnroll.IdeSupport.Common.ProjectSystem.Configuration;

public interface IDeveroomConfigurationProvider
{
    /// <summary>Raised on any thread when configuration changes.</summary>
    event EventHandler ConfigurationChanged;
    DeveroomConfiguration GetConfiguration();
}
