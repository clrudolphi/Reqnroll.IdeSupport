using System;

namespace Reqnroll.IdeSupport.Common.ProjectSystem;

public static class PathUtils
{
    /// <summary>
    /// True when <paramref name="filePath"/> is <paramref name="folder"/> itself, or lives
    /// somewhere under it — with a directory-separator boundary check, unlike a bare
    /// <c>filePath.StartsWith(folder)</c>.
    /// </summary>
    /// <remarks>
    /// A plain string-prefix check treats a sibling folder whose name happens to extend the
    /// prefix as "inside" it — e.g. <c>@"C:\Repo\Minimalnet481\Foo.cs"</c> starts with
    /// <c>@"C:\Repo\Minimal"</c> even though <c>Minimalnet481</c> is a completely different
    /// folder than <c>Minimal</c>. Confirmed live: this let a sibling project's step-definition
    /// bindings bleed into another project's registry, producing false "ambiguous step" matches.
    /// </remarks>
    public static bool IsUnderFolder(string? filePath, string? folder)
    {
        if (string.IsNullOrEmpty(filePath) || string.IsNullOrEmpty(folder))
            return false;

        var normalizedFolder = folder!.TrimEnd('\\', '/');
        if (!filePath!.StartsWith(normalizedFolder, StringComparison.OrdinalIgnoreCase))
            return false;

        if (filePath.Length == normalizedFolder.Length)
            return true; // filePath IS the folder

        var boundaryChar = filePath[normalizedFolder.Length];
        return boundaryChar is '\\' or '/';
    }
}
