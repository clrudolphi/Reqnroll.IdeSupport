#nullable disable

namespace Reqnroll.IdeSupport.Common;

/// <summary>FileSystemExtensions</summary>
public static class FileSystemExtensions
{
    /// <summary>Gets or sets the get file path if exists.</summary>
    public static string GetFilePathIfExists(this IFileSystemForIDE fileSystem, string filePath)
    {
        if (fileSystem.File.Exists(filePath))
            return filePath;
        return null;
    }
}
