using Reqnroll.IdeSupport.Common.Configuration;

namespace Reqnroll.IdeSupport.Common.ProjectSystem.Configuration;

public static class ConfigurationProjectSystemExtensions
{
    public static DeveroomConfiguration GetDeveroomConfiguration(this IProjectScope projectScope)
    {
        var provider = GetDeveroomConfigurationProvider(projectScope);
        return provider.GetConfiguration();
    }

    public static IDeveroomConfigurationProvider GetDeveroomConfigurationProvider(this IProjectScope projectScope)
    {
        return (IDeveroomConfigurationProvider)projectScope.Properties.GetOrAdd(typeof(IDeveroomConfigurationProvider), _ =>
            new ProjectScopeDeveroomConfigurationProvider(projectScope));
    }

    public static DeveroomConfiguration GetDeveroomConfiguration(this IIdeScope ideScope, IProjectScope projectScope)
    {
        var provider = ideScope.GetDeveroomConfigurationProvider(projectScope);
        return provider.GetConfiguration();
    }

    public static IDeveroomConfigurationProvider GetDeveroomConfigurationProvider(this IIdeScope ideScope,
        IProjectScope projectScope)
    {
        if (projectScope != null)
            return projectScope.GetDeveroomConfigurationProvider();
        return new ProjectSystemDeveroomConfigurationProvider(ideScope);
    }
}
