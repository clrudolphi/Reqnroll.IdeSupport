
namespace Reqnroll.IdeSupport.Common.ProjectSystem.Settings;

/// <summary>ProjectSettingsProjectSystemExtensions</summary>
public static class ProjectSettingsProjectSystemExtensions
{
    /// <summary>Returns the resolved project settings for the given project scope.</summary>
    public static ProjectSettings GetProjectSettings(this IProjectScope projectScope)
    {
        var provider = GetProjectSettingsProvider(projectScope);
        return provider.GetProjectSettings();
    }

    /// <summary>Returns the project settings provider for the given project scope, creating and caching one if none exists yet.</summary>
    public static IProjectSettingsProvider GetProjectSettingsProvider(this IProjectScope projectScope)
    {
        return projectScope.Properties.GetOrAdd(typeof(IProjectSettingsProvider), _ =>
            new ProjectSettingsProvider(projectScope, new ReqnrollProjectSettingsProvider(projectScope))) as IProjectSettingsProvider;
    }
}
