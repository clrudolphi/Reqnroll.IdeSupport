#nullable disable

namespace Reqnroll.IDE.Common;

public static class FileSystemExtensions
{
    public static string GetFilePathIfExists(this IFileSystemForIDE fileSystem, string filePath)
    {
        if (fileSystem.File.Exists(filePath))
            return filePath;
        return null;
    }
}
