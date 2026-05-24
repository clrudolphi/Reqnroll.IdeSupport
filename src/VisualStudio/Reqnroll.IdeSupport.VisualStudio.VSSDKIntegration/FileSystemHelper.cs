#nullable disable

namespace Reqnroll.VisualStudio.Common;

public static class FileSystemHelper
{
    public static bool IsOfType(string filePath, string extension)
    {
        if (extension == null)
            return true;
        return filePath.EndsWith(extension, StringComparison.OrdinalIgnoreCase);
    }
}