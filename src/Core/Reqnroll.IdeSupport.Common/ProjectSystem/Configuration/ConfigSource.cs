using Reqnroll.IdeSupport.Common.Logging;
using System;

namespace Reqnroll.IdeSupport.Common.ProjectSystem.Configuration;

/// <summary>Initializes a new instance of the <see cref="ConfigSource"/> class.</summary>
/// <summary>Gets or sets the last change time.</summary>
/// <summary>Gets or sets the file path.</summary>
/// <summary>Gets or sets the error message.</summary>
/// <summary>ConfigSource</summary>
public record ConfigSource(string FilePath, DateTimeOffset LastChangeTime, string? ErrorMessage)
{
    /// <summary>Gets or sets the is valid.</summary>
    public bool IsValid => !string.IsNullOrEmpty(FilePath);

    /// <summary>Gets or sets the try get config source.</summary>
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
    /// <summary>Gets or sets the create invalid.</summary>
    public static ConfigSource CreateInvalid(string errorMessage)
    {
        return new(string.Empty, DateTimeOffset.MinValue, errorMessage);
    }

    /// <summary>Gets or sets the create valid.</summary>
    public static ConfigSource CreateValid(string filePath, DateTimeOffset lastChangeTime)
    {
        return new(filePath, lastChangeTime, null);
    }
}
