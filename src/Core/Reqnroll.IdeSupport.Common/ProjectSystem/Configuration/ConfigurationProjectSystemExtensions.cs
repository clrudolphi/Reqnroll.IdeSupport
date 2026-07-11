using Reqnroll.IdeSupport.Common.Configuration;

namespace Reqnroll.IdeSupport.Common.ProjectSystem.Configuration;

/// <summary>ConfigurationProjectSystemExtensions</summary>
public static class ConfigurationProjectSystemExtensions
{
    /// <summary>Returns the resolved Deveroom configuration for the given project scope.</summary>
    public static DeveroomConfiguration GetDeveroomConfiguration(this IProjectScope projectScope)
    {
        var provider = GetDeveroomConfigurationProvider(projectScope);
        return provider.GetConfiguration();
    }

    /// <summary>Returns the configuration provider for the given project scope, creating and caching one if none exists yet.</summary>
    public static IDeveroomConfigurationProvider GetDeveroomConfigurationProvider(this IProjectScope projectScope)
    {
        return (IDeveroomConfigurationProvider)projectScope.Properties.GetOrAdd(typeof(IDeveroomConfigurationProvider), _ =>
            new ProjectScopeDeveroomConfigurationProvider(projectScope));
    }

    /// <summary>Returns the resolved Deveroom configuration for the given IDE/project scope pair.</summary>
    public static DeveroomConfiguration GetDeveroomConfiguration(this IIdeScope ideScope, IProjectScope projectScope)
    {
        var provider = ideScope.GetDeveroomConfigurationProvider(projectScope);
        return provider.GetConfiguration();
    }

    /// <summary>Returns the configuration provider for the given project scope, falling back to an IDE-scoped provider if <paramref name="projectScope"/> is <c>null</c>.</summary>
    public static IDeveroomConfigurationProvider GetDeveroomConfigurationProvider(this IIdeScope ideScope,
        IProjectScope projectScope)
    {
        if (projectScope != null)
            return projectScope.GetDeveroomConfigurationProvider();
        return new ProjectSystemDeveroomConfigurationProvider(ideScope);
    }
}
