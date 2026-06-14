namespace Reqnroll.IdeSupport.LSP.Core.Editor.Services.Commenting;

/// <summary>
/// Protocol-agnostic edit describing a single line's replacement text.
/// Both lines are 0-based inclusive.
/// </summary>
public sealed record GherkinCommentEdit(
    int StartLine,
    int EndLine,
    string NewText);

/// <summary>
/// The result of a comment-toggle operation: a set of per-line text replacements.
/// </summary>
public sealed record GherkinCommentToggleResult(
    IReadOnlyList<GherkinCommentEdit> Edits);
