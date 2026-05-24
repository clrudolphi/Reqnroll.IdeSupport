
namespace Reqnroll.IdeSupport.Common.ProjectSystem.Settings;

public static class ProjectSettingsProjectSystemExtensions
{
    public static ProjectSettings GetProjectSettings(this IProjectScope projectScope)
    {
        var provider = GetProjectSettingsProvider(projectScope);
        return provider.GetProjectSettings();
    }

    public static IProjectSettingsProvider GetProjectSettingsProvider(this IProjectScope projectScope)
    {
        return projectScope.Properties.GetOrAdd(typeof(IProjectSettingsProvider), _ =>
            new ProjectSettingsProvider(projectScope, new ReqnrollProjectSettingsProvider(projectScope))) as IProjectSettingsProvider;
    }
}
