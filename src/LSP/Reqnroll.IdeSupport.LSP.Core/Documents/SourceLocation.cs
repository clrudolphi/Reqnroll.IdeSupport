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

    /// <summary>Gets or sets the source file.</summary>
    public string SourceFile { get; }
    /// <summary>Gets or sets the source file line.</summary>
    public int SourceFileLine { get; } // 1-based
    /// <summary>Gets or sets the source file column.</summary>
    public int SourceFileColumn { get; } // 1-based
    /// <summary>Gets or sets the source file end line.</summary>
    public int? SourceFileEndLine { get; } // 1-based
    /// <summary>Gets or sets the source file end column.</summary>
    public int? SourceFileEndColumn { get; } // 1-based

    /// <summary>Gets or sets the has end position.</summary>
    public bool HasEndPosition => SourceFileEndLine != null && SourceFileEndColumn != null;

    /// <summary>Returns <see langword="true"/> when <paramref name="line1Based"/> falls within
    /// the span [<see cref="SourceFileLine"/>, <see cref="SourceFileEndLine"/>].</summary>
    public bool ContainsLine(int line1Based)
    {
        var endLine = SourceFileEndLine ?? SourceFileLine;
        return line1Based >= SourceFileLine && line1Based <= endLine;
    }

    /// <summary>Gets or sets the to string.</summary>
    public override string ToString() => $"{SourceFile}({SourceFileLine},{SourceFileColumn})";
}
