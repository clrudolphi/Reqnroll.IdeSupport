using Reqnroll.IdeSupport.Common.Configuration;
using System;

namespace Reqnroll.IdeSupport.Common.ProjectSystem.Configuration;

/// <summary>IDeveroomConfigurationProvider</summary>
public interface IDeveroomConfigurationProvider
{
    /// <summary>Raised on any thread when configuration changes.</summary>
    event EventHandler ConfigurationChanged;
    /// <summary>Gets or sets the get configuration.</summary>
    DeveroomConfiguration GetConfiguration();
}
