namespace Reqnroll.IdeSupport.LSP.Core.Editor.Services.Commenting;

/// <summary>
/// Toggles <c>#</c> comments on Gherkin feature file lines.
/// If every line in the range is already commented, all are uncommented.
/// Otherwise all are commented (regardless of per-line state).
/// </summary>
public class CommentToggleService : ICommentToggleService
{
    private const char CommentChar = '#';

    public GherkinCommentToggleResult ToggleComment(
        string documentText,
        int rangeStartLine,
        int rangeEndLine)
    {
        var lines = SplitLines(documentText);
        var allCommented = true;

        // Determine if ALL non-empty lines in range are commented.
        // Empty lines count as "not commented" for toggle determination.
        for (int i = rangeStartLine; i <= rangeEndLine && i < lines.Length; i++)
        {
            var trimmed = lines[i].TrimStart();
            if (trimmed.Length == 0)
            {
                allCommented = false;
                break;
            }
            if (trimmed[0] != CommentChar)
            {
                allCommented = false;
                break;
            }
        }

        var edits = new List<GherkinCommentEdit>();
        for (int i = rangeStartLine; i <= rangeEndLine && i < lines.Length; i++)
        {
            var line = lines[i];
            var newLine = allCommented ? UncommentLine(line) : CommentLine(line);
            edits.Add(new GherkinCommentEdit(i, i, newLine));
        }

        return new GherkinCommentToggleResult(edits.AsReadOnly());
    }

    private static string CommentLine(string line)
    {
        var trimmed = line.TrimStart();
        if (trimmed.Length == 0)
            return CommentChar.ToString();
        return CommentChar + " " + line;
    }

    private static string UncommentLine(string line)
    {
        var trimmed = line.TrimStart();
        if (!trimmed.StartsWith(CommentChar.ToString()))
            return line;

        var leadingSpaces = line.Length - trimmed.Length;
        var afterHash = trimmed.Substring(1); // remove the #

        // Remove one following space if present
        var content = afterHash.StartsWith(" ") ? afterHash.Substring(1) : afterHash;

        return new string(' ', leadingSpaces) + content;
    }

    private static string[] SplitLines(string text)
        => text.Replace("\r\n", "\n").Split('\n');
}
