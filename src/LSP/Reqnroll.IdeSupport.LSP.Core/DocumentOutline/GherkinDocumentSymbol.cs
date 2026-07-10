using Reqnroll.IdeSupport.LSP.Core.Documents;

namespace Reqnroll.IdeSupport.LSP.Core.DocumentOutline;

/// <summary>GherkinSymbolKind</summary>
public enum GherkinSymbolKind
{
    /// <summary>Gets or sets the feature.</summary>
    Feature,
    /// <summary>Gets or sets the background.</summary>
    Background,
    /// <summary>Gets or sets the rule.</summary>
    Rule,
    /// <summary>Gets or sets the scenario.</summary>
    Scenario,
    /// <summary>Gets or sets the scenario outline.</summary>
    ScenarioOutline,
    /// <summary>Gets or sets the step.</summary>
    Step,
    /// <summary>Gets or sets the examples.</summary>
    Examples,
}

/// <summary>GherkinDocumentSymbol</summary>
public record GherkinDocumentSymbol(
    /// <summary>Gets or sets the name.</summary>
    string Name,
    /// <summary>Gets or sets the detail.</summary>
    string? Detail,
    /// <summary>Gets or sets the kind.</summary>
    GherkinSymbolKind Kind,
    /// <summary>Gets or sets the range.</summary>
    GherkinRange Range,
    /// <summary>Gets or sets the selection range.</summary>
    GherkinRange SelectionRange,
    /// <summary>Gets or sets the children.</summary>
    IReadOnlyList<GherkinDocumentSymbol> Children);
