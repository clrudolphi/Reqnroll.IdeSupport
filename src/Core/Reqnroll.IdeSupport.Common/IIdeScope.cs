using Reqnroll.IdeSupport.Common.Logging;
using Reqnroll.IdeSupport.Common.Telemetry;
using System;

namespace Reqnroll.IdeSupport.Common;

/// <summary>
/// Minimal IDE scope contract needed by the Wizards layer.
/// The full IIdeScope from Reqnroll.IdeSupport.VisualStudio has ~15 members (text buffers,
/// Roslyn, solution events, etc.) that wizards do not need.
/// </summary>
public interface IIdeScope
{
    /// <summary>Gets or sets the is solution loaded.</summary>
    bool IsSolutionLoaded { get; }
    /// <summary>Gets or sets the logger.</summary>
    IIdeSupportLogger Logger { get; }
    /// <summary>Gets or sets the telemetry service.</summary>
    ITelemetryService TelemetryService { get; }
    /// <summary>Gets or sets the actions.</summary>
    IIdeActions Actions { get; }
    /// <summary>Gets or sets the file system.</summary>
    IFileSystemForIDE FileSystem { get; }
}

/// <summary>
/// Minimal actions contract: only error/problem reporting used by wizards.
/// </summary>
public interface IIdeActions
{
    /// <summary>Gets or sets the show error.</summary>
    void ShowError(string description, Exception exception);
    /// <summary>Gets or sets the show problem.</summary>
    void ShowProblem(string message);
}