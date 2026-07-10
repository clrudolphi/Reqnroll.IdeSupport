using Reqnroll.IdeSupport.Common.Configuration;

namespace Reqnroll.IdeSupport.Common.ProjectSystem.Configuration;

/// <summary>ConfigurationProjectSystemExtensions</summary>
public static class ConfigurationProjectSystemExtensions
{
    /// <summary>Gets or sets the get deveroom configuration.</summary>
    public static DeveroomConfiguration GetDeveroomConfiguration(this IProjectScope projectScope)
    {
        var provider = GetDeveroomConfigurationProvider(projectScope);
        return provider.GetConfiguration();
    }

    /// <summary>Gets or sets the get deveroom configuration provider.</summary>
    public static IDeveroomConfigurationProvider GetDeveroomConfigurationProvider(this IProjectScope projectScope)
    {
        return (IDeveroomConfigurationProvider)projectScope.Properties.GetOrAdd(typeof(IDeveroomConfigurationProvider), _ =>
            new ProjectScopeDeveroomConfigurationProvider(projectScope));
    }

    /// <summary>Gets or sets the get deveroom configuration.</summary>
    public static DeveroomConfiguration GetDeveroomConfiguration(this IIdeScope ideScope, IProjectScope projectScope)
    {
        var provider = ideScope.GetDeveroomConfigurationProvider(projectScope);
        return provider.GetConfiguration();
    }

    /// <summary>Gets or sets the get deveroom configuration provider.</summary>
    public static IDeveroomConfigurationProvider GetDeveroomConfigurationProvider(this IIdeScope ideScope,
        IProjectScope projectScope)
    {
        if (projectScope != null)
            return projectScope.GetDeveroomConfigurationProvider();
        return new ProjectSystemDeveroomConfigurationProvider(ideScope);
    }
}
