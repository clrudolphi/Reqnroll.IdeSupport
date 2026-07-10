#nullable enable


using System;

namespace Reqnroll.IdeSupport.Common.ProjectSystem.Settings;

/// <summary>IProjectSettingsProvider</summary>
public interface IProjectSettingsProvider
{
    //event EventHandler<EventArgs> WeakSettingsInitialized;
    //event EventHandler<EventArgs> SettingsInitialized;

    /// <summary>Gets or sets the get project settings.</summary>
    ProjectSettings GetProjectSettings();
    /// <summary>Gets or sets the check project settings.</summary>
    ProjectSettings CheckProjectSettings();
}
