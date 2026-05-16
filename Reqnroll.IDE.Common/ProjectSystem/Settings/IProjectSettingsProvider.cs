#nullable enable


using System;

namespace Reqnroll.IDE.Common.ProjectSystem.Settings;

public interface IProjectSettingsProvider
{
    //event EventHandler<EventArgs> WeakSettingsInitialized;
    //event EventHandler<EventArgs> SettingsInitialized;

    ProjectSettings GetProjectSettings();
    ProjectSettings CheckProjectSettings();
}
