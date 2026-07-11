namespace Reqnroll.IdeSupport.LSP.Core.Documents;

/// <summary>
/// An immutable, line-indexed view of an LSP text document's content at a given version,
/// lazily splitting the text into lines (handling \r, \n, and \r\n) on first access.
/// </summary>
public class LspTextSnapshot : IGherkinTextSnapshot
{
    private readonly string _text;
    private readonly Lazy<IReadOnlyList<IGherkinTextSnapshotLine>> _lines;

    /// <summary>Creates a snapshot of a document's text at a given version.</summary>
    public LspTextSnapshot(string uri, int version, string text)
    {
        Uri = uri;
        Version = version;
        _text = text;
        _lines = new Lazy<IReadOnlyList<IGherkinTextSnapshotLine>>(BuildLines);
    }

    /// <summary>The document's URI.</summary>
    public string Uri { get; }
    /// <summary>The document version this snapshot was taken at.</summary>
    public int Version { get; }
    /// <summary>Returns the full document text.</summary>
    public string GetText() => _text;
    /// <summary>The number of lines in the document.</summary>
    public int LineCount => _lines.Value.Count;

    /// <summary>The total length, in characters, of the document text.</summary>
    public int Length => _text.Length;

    /// <summary>Returns the line at <paramref name="lineNumber"/>, clamped to the last line if out of range.</summary>
    public IGherkinTextSnapshotLine GetLineFromLineNumber(int lineNumber) =>
        _lines.Value[Math.Min(lineNumber, LineCount - 1)];

    private IReadOnlyList<IGherkinTextSnapshotLine> BuildLines()
    {
        var lines = new List<IGherkinTextSnapshotLine>();
        int lineStart = 0;
        int lineNumber = 0;
        for (int i = 0; i < _text.Length;)
        {
            int lineBreakLen = 0;
            if (_text[i] == '\r')
            {
                if (i + 1 < _text.Length && _text[i + 1] == '\n')
                    lineBreakLen = 2;
                else
                    lineBreakLen = 1;
            }
            else if (_text[i] == '\n')
            {
                lineBreakLen = 1;
            }

            if (lineBreakLen > 0)
            {
                int length = i - lineStart;
                lines.Add(new LspTextSnapshotLine(this, lineNumber++, lineStart, i));
                i += lineBreakLen;
                lineStart = i;
            }
            else
            {
                i++;
            }
        }

        // Add last line if text doesn't end with a line break
        if (lineStart <= _text.Length)
        {
            int length = _text.Length - lineStart;
            lines.Add(new LspTextSnapshotLine(this, lineNumber, lineStart, _text.Length));
        }

        return lines;
    }
}

/// <summary>A single line's span (character offsets) within an <see cref="LspTextSnapshot"/>, excluding its line break.</summary>
public class LspTextSnapshotLine : IGherkinTextSnapshotLine
{
    private readonly IGherkinTextSnapshot _snapshot;
    private readonly int _start;
    private readonly int _end;

    /// <summary>Creates a line descriptor for the given snapshot and character range.</summary>
    public LspTextSnapshotLine(IGherkinTextSnapshot snapshot, int lineNumber, int start, int end)
    {
        _snapshot = snapshot;
        _start = start;
        _end = end;
        LineNumber = lineNumber;
    }

    /// <summary>The zero-based line number.</summary>
    public int LineNumber { get; }
    /// <summary>The character offset where the line starts within the document.</summary>
    public int Start => _start;
    /// <summary>The character offset where the line ends (exclusive of the line break) within the document.</summary>
    public int End => _end;
    /// <summary>Returns this line's text, excluding its line break.</summary>
    public string GetText() => _snapshot.GetText().Substring(_start, _end - _start);
}