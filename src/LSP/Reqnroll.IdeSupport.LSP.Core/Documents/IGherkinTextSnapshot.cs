namespace Reqnroll.IdeSupport.LSP.Core.Documents;

/// <summary>
/// VS-free, immutable, versioned document snapshot.
/// LSP implementation: LspTextSnapshot, constructed from textDocument/didChange content.
/// </summary>
public interface IGherkinTextSnapshot
{
    /// <summary>Gets the version number of this text snapshot.</summary>
    int Version { get; }
    /// <summary>Gets the number of lines in the snapshot.</summary>
    int LineCount { get; }
    /// <summary>Gets the total character length of the snapshot.</summary>
    int Length { get; }
    /// <summary>Gets the full text of the snapshot.</summary>
    string GetText();
    /// <summary>Gets the line at the given zero-based line number.</summary>
    IGherkinTextSnapshotLine GetLineFromLineNumber(int lineNumber); // 0-based
}

/// <summary>IGherkinTextSnapshotLine</summary>
public interface IGherkinTextSnapshotLine
{
    /// <summary>Gets the zero-based line number.</summary>
    int LineNumber { get; }           // 0-based
    /// <summary>Gets the character offset of the start of the line from the start of the document.</summary>
    int Start { get; }                // char offset from document start
    /// <summary>Gets the character offset of the end of the line, inclusive of line-break characters.</summary>
    int End { get; }                  // incluise of line break chars
    /// <summary>Gets the text of this line.</summary>
    string GetText();
}