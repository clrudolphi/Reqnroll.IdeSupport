using Reqnroll.IdeSupport.Common.Logging;
using System;

namespace Reqnroll.IdeSupport.Common.ProjectSystem.Configuration;

/// <summary>
/// Represents the resolved location and last-known-good state of a project's config source file
/// (e.g. the test assembly used to detect Reqnroll configuration), or an invalid/error state when
/// the file could not be found or read.
/// </summary>
/// <param name="FilePath">The resolved path to the config source file, or empty when invalid.</param>
/// <param name="LastChangeTime">The last write time of the file, used to detect staleness.</param>
/// <param name="ErrorMessage">A user-facing explanation when the source is invalid; <see langword="null"/> when valid.</param>
public record ConfigSource(string FilePath, DateTimeOffset LastChangeTime, string? ErrorMessage)
{
    /// <summary>Gets whether the source resolved to a usable file path.</summary>
    public bool IsValid => !string.IsNullOrEmpty(FilePath);

    /// <summary>
    /// Attempts to resolve a <see cref="ConfigSource"/> for <paramref name="filePath"/>, returning an
    /// invalid instance with a descriptive <see cref="ErrorMessage"/> if the path is empty, the file
    /// doesn't exist, or it can't be accessed.
    /// </summary>
    public static ConfigSource TryGetConfigSource(string filePath, IFileSystemForIDE fileSystem, IIdeSupportLogger logger)
    {
        if (string.IsNullOrEmpty(filePath))
            return CreateInvalid("Test assembly path could not be detected, therefore some Reqnroll Visual Studio Extension features are disabled. Try to rebuild the project or restart Visual Studio.");
        if (!fileSystem.File.Exists(filePath))
            return CreateInvalid("Test assembly not found. Please build the project to enable the Reqnroll Visual Studio Extension features.");
        try
        {
            return CreateValid(filePath, fileSystem.File.GetLastWriteTimeUtc(filePath));
        }
        catch (Exception ex)
        {
            logger.LogDebugException(ex);
            return CreateInvalid($"Test assembly could not be accessed: {ex.Message}. Please rebuild the project to enable the Reqnroll Visual Studio Extension features.");
        }
    }
    /// <summary>Creates an invalid <see cref="ConfigSource"/> carrying the given error message.</summary>
    public static ConfigSource CreateInvalid(string errorMessage)
    {
        return new(string.Empty, DateTimeOffset.MinValue, errorMessage);
    }

    /// <summary>Creates a valid <see cref="ConfigSource"/> for a successfully resolved file.</summary>
    public static ConfigSource CreateValid(string filePath, DateTimeOffset lastChangeTime)
    {
        return new(filePath, lastChangeTime, null);
    }
}
