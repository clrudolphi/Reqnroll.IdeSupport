namespace Reqnroll.IdeSupport.LSP.Core.Folding;

/// <summary>
/// Protocol-agnostic model for a folding range region in a Gherkin document.
/// Lines are 0-based and inclusive (LSP FoldingRange convention).
/// </summary>
public sealed record GherkinFoldingRange(
    int StartLine,
    int EndLine,
    GherkinFoldingRangeKind? Kind = null);

/// <summary>
/// Categorised kinds of folding ranges (maps to LSP FoldingRangeKind).
/// </summary>
public enum GherkinFoldingRangeKind
{
    Comment,
    Imports,
    Region
}
