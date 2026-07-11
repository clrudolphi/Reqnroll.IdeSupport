using System.IO;

namespace Reqnroll.IdeSupport.Common;

/// <summary>FileDetails</summary>
public record FileDetails
{
    private readonly FileInfo _file;

    private FileDetails(FileInfo file)
    {
        _file = file;
    }

    /// <summary>Gets the fully-qualified path of the file.</summary>
    public string FullName => _file.FullName;
    /// <summary>Gets the file name, including its extension.</summary>
    public string Name => _file.Name;

    /// <summary>Creates a <see cref="FileDetails"/> for the given path.</summary>
    public static FileDetails FromPath(string path) => new(new FileInfo(path));
    /// <summary>Creates a <see cref="FileDetails"/> for the path formed by combining two path segments.</summary>
    public static FileDetails FromPath(string path1, string path2) => FromPath(Path.Combine(path1, path2));
    /// <summary>Implicitly converts a <see cref="FileDetails"/> to its full path string.</summary>
    public static implicit operator string(FileDetails path) => path.FullName;

    /// <summary>Returns the full path of the file.</summary>
    public override string ToString() => _file.FullName;
}
