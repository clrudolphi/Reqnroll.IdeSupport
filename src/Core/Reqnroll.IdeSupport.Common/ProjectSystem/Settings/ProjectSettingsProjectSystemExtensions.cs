
namespace Reqnroll.IdeSupport.Common.ProjectSystem.Settings;

/// <summary>ProjectSettingsProjectSystemExtensions</summary>
public static class ProjectSettingsProjectSystemExtensions
{
    /// <summary>Gets or sets the get project settings.</summary>
    public static ProjectSettings GetProjectSettings(this IProjectScope projectScope)
    {
        var provider = GetProjectSettingsProvider(projectScope);
        return provider.GetProjectSettings();
    }

    /// <summary>Gets or sets the get project settings provider.</summary>
    public static IProjectSettingsProvider GetProjectSettingsProvider(this IProjectScope projectScope)
    {
        return projectScope.Properties.GetOrAdd(typeof(IProjectSettingsProvider), _ =>
            new ProjectSettingsProvider(projectScope, new ReqnrollProjectSettingsProvider(projectScope))) as IProjectSettingsProvider;
    }
}
