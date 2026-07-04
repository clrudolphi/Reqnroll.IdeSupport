using Reqnroll.IdeSupport.LSP.Core.Documents;

namespace Reqnroll.IdeSupport.LSP.Core.InlayHints;

public enum GherkinInlayHintKind
{
    /// <summary>A single, unambiguous binding matches the step.</summary>
    Binding,

    /// <summary>The step text itself matches more than one binding (same concrete text).</summary>
    Ambiguous,

    /// <summary>
    /// A Scenario Outline/Background template step resolves to more than one distinct binding
    /// across its example rows, without any single row being ambiguous on its own.
    /// </summary>
    Templated,
}

/// <summary>
/// A single inline annotation for a step, projected from the binding match cache (F23).
/// <see cref="AnchorRange"/> is the step's own text span; callers paint the hint at its end.
/// </summary>
public sealed record GherkinInlayHint(
    GherkinRange AnchorRange,
    string Label,
    string? Tooltip,
    GherkinInlayHintKind Kind);
