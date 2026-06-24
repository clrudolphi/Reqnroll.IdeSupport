namespace Reqnroll.IdeSupport.LSP.Core.Commenting;

/// <summary>
/// Toggles Gherkin comments (<c>#</c>) on a range of lines in a feature-file document.
/// </summary>
public interface ICommentToggleService
{
    /// <summary>
    /// Toggles the comment state for lines <paramref name="rangeStartLine"/> to
    /// <paramref name="rangeEndLine"/> (0-based, inclusive).
    /// If ALL lines in the range are commented (start with <c>#</c>), they are
    /// uncommented. Otherwise ALL lines are commented.
    /// </summary>
    GherkinCommentToggleResult ToggleComment(
        string documentText,
        int rangeStartLine,
        int rangeEndLine);
}
