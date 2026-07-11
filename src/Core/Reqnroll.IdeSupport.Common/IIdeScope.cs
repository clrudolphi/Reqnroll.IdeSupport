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
    /// <summary>Gets whether a solution is currently loaded in the IDE.</summary>
    bool IsSolutionLoaded { get; }
    /// <summary>Gets the logger for this IDE scope.</summary>
    IIdeSupportLogger Logger { get; }
    /// <summary>Gets the telemetry service for this IDE scope.</summary>
    ITelemetryService TelemetryService { get; }
    /// <summary>Gets the IDE actions (error/problem reporting) available to this scope.</summary>
    IIdeActions Actions { get; }
    /// <summary>Gets the file system abstraction for this IDE scope.</summary>
    IFileSystemForIDE FileSystem { get; }
}

/// <summary>
/// Minimal actions contract: only error/problem reporting used by wizards.
/// </summary>
public interface IIdeActions
{
    /// <summary>Reports an error, described by <paramref name="description"/>, caused by <paramref name="exception"/>.</summary>
    void ShowError(string description, Exception exception);
    /// <summary>Reports a non-exception problem message to the user.</summary>
    void ShowProblem(string message);
}