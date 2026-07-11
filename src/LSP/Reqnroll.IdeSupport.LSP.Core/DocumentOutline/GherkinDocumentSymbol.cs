using Reqnroll.IdeSupport.LSP.Core.Documents;

namespace Reqnroll.IdeSupport.LSP.Core.DocumentOutline;

/// <summary>GherkinSymbolKind</summary>
public enum GherkinSymbolKind
{
    /// <summary>Feature</summary>
    Feature,
    /// <summary>Background</summary>
    Background,
    /// <summary>Rule</summary>
    Rule,
    /// <summary>Scenario</summary>
    Scenario,
    /// <summary>ScenarioOutline</summary>
    ScenarioOutline,
    /// <summary>Step</summary>
    Step,
    /// <summary>Examples</summary>
    Examples,
}

/// <summary>Represents a symbol in the Gherkin document outline.</summary>
/// <param name="Name">The symbol name.</param>
/// <param name="Detail">Optional detail text.</param>
/// <param name="Kind">The symbol kind.</param>
/// <param name="Range">The full range of the symbol.</param>
/// <param name="SelectionRange">The selection range of the symbol.</param>
/// <param name="Children">Nested child symbols.</param>
public record GherkinDocumentSymbol(
    string Name,
    string? Detail,
    GherkinSymbolKind Kind,
    GherkinRange Range,
    GherkinRange SelectionRange,
    IReadOnlyList<GherkinDocumentSymbol> Children);