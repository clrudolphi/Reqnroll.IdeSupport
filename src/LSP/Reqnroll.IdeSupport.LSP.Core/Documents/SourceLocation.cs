#nullable disable
namespace Reqnroll.IdeSupport.LSP.Core.Documents;

/// <summary>SourceLocation</summary>
public class SourceLocation
{
    /// <summary>Initializes a new instance of the <see cref="SourceLocation"/> class.</summary>
    public SourceLocation(string sourceFile, int sourceFileLine, int sourceFileColumn, int? sourceFileEndLine = null,
        int? sourceFileEndColumn = null)
    {
        SourceFile = sourceFile;
        SourceFileLine = sourceFileLine;
        SourceFileColumn = sourceFileColumn;
        SourceFileEndLine = sourceFileEndLine;
        SourceFileEndColumn = sourceFileEndColumn;
    }

    /// <summary>Gets the path of the source file.</summary>
    public string SourceFile { get; }
    /// <summary>Gets the 1-based line number where the location begins.</summary>
    public int SourceFileLine { get; } // 1-based
    /// <summary>Gets the 1-based column number where the location begins.</summary>
    public int SourceFileColumn { get; } // 1-based
    /// <summary>Gets the 1-based line number where the location ends, if known.</summary>
    public int? SourceFileEndLine { get; } // 1-based
    /// <summary>Gets the 1-based column number where the location ends, if known.</summary>
    public int? SourceFileEndColumn { get; } // 1-based

    /// <summary>Gets whether both an end line and end column are set.</summary>
    public bool HasEndPosition => SourceFileEndLine != null && SourceFileEndColumn != null;

    /// <summary>Returns <see langword="true"/> when <paramref name="line1Based"/> falls within
    /// the span [<see cref="SourceFileLine"/>, <see cref="SourceFileEndLine"/>].</summary>
    public bool ContainsLine(int line1Based)
    {
        var endLine = SourceFileEndLine ?? SourceFileLine;
        return line1Based >= SourceFileLine && line1Based <= endLine;
    }

    /// <summary>Formats the location as <c>file(line,column)</c>.</summary>
    public override string ToString() => $"{SourceFile}({SourceFileLine},{SourceFileColumn})";
}
