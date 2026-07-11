#nullable disable

namespace Reqnroll.IdeSupport.Common;

/// <summary>FileSystemExtensions</summary>
public static class FileSystemExtensions
{
    /// <summary>Returns <paramref name="filePath"/> if the file exists, otherwise <c>null</c>.</summary>
    public static string GetFilePathIfExists(this IFileSystemForIDE fileSystem, string filePath)
    {
        if (fileSystem.File.Exists(filePath))
            return filePath;
        return null;
    }
}
