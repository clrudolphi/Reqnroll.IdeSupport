namespace Reqnroll.IdeSupport.LSP.Core.Documents;

/// <summary>
/// VS-free, immutable, versioned document snapshot.
/// LSP implementation: LspTextSnapshot, constructed from textDocument/didChange content.
/// </summary>
public interface IGherkinTextSnapshot
{
    /// <summary>Gets or sets the version.</summary>
    int Version { get; }
    /// <summary>Gets or sets the line count.</summary>
    int LineCount { get; }
    /// <summary>Gets or sets the length.</summary>
    int Length { get; }
    /// <summary>Gets or sets the get text.</summary>
    string GetText();
    /// <summary>Gets or sets the get line from line number.</summary>
    IGherkinTextSnapshotLine GetLineFromLineNumber(int lineNumber); // 0-based
}

/// <summary>IGherkinTextSnapshotLine</summary>
public interface IGherkinTextSnapshotLine
{
    /// <summary>Gets or sets the line number.</summary>
    int LineNumber { get; }           // 0-based
    /// <summary>Gets or sets the start.</summary>
    int Start { get; }                // char offset from document start
    /// <summary>Gets or sets the end.</summary>
    int End { get; }                  // incluise of line break chars   
    /// <summary>Gets or sets the get text.</summary>
    string GetText();
}