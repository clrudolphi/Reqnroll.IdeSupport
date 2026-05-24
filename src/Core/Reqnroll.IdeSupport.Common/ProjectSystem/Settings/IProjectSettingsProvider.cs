#nullable enable


using System;

namespace Reqnroll.IdeSupport.Common.ProjectSystem.Settings;

public interface IProjectSettingsProvider
{
    //event EventHandler<EventArgs> WeakSettingsInitialized;
    //event EventHandler<EventArgs> SettingsInitialized;

    ProjectSettings GetProjectSettings();
    ProjectSettings CheckProjectSettings();
}
