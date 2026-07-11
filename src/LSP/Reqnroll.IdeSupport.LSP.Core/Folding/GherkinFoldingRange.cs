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
    /// <summary>A comment block region.</summary>
    Comment,
    /// <summary>An imports/using-declarations region.</summary>
    Imports,
    /// <summary>A generic named region (e.g. a Gherkin block such as a scenario, rule, or examples table).</summary>
    Region
}
