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

    /// <summary>Gets or sets the full name.</summary>
    public string FullName => _file.FullName;
    /// <summary>Gets or sets the name.</summary>
    public string Name => _file.Name;

    /// <summary>Gets or sets the from path.</summary>
    public static FileDetails FromPath(string path) => new(new FileInfo(path));
    /// <summary>Gets or sets the from path.</summary>
    public static FileDetails FromPath(string path1, string path2) => FromPath(Path.Combine(path1, path2));
    /// <summary>Gets or sets the implicit operator string.</summary>
    public static implicit operator string(FileDetails path) => path.FullName;

    /// <summary>Gets or sets the to string.</summary>
    public override string ToString() => _file.FullName;
}
