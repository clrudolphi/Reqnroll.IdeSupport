#nullable enable

using System.Collections.Generic;
using System.Linq;

namespace Reqnroll.IdeSupport.VisualStudio.NavigationBar;

/// <summary>
/// Pure combo-population logic for the Navigation Bar, kept separate
/// from <c>GherkinDropdownBarClient</c>'s COM surface so it's unit-testable without a live VS host.
/// </summary>
internal static class GherkinNavigationBarLayout
{
    /// <summary>
    /// Combo 0 ("structure") entries: every container node (Feature/Rule/Background/Scenario/
    /// ScenarioOutline) in document order, flattened out of the nested symbol tree.
    /// </summary>
    public static IReadOnlyList<GherkinSymbolNode> BuildStructureEntries(IReadOnlyList<GherkinSymbolNode> roots) =>
        Flatten(roots).Where(n => GherkinSymbolKinds.IsContainer(n.Kind)).ToList();

    /// <summary>Index into <see cref="BuildStructureEntries"/> for the container enclosing the caret, or -1.</summary>
    public static int FindSelectedStructureIndex(IReadOnlyList<GherkinSymbolNode> structureEntries, int caretLine)
    {
        var deepest = FindDeepestContainer(structureEntries, caretLine);
        return deepest is null ? -1 : structureEntries.ToList().FindIndex(n => ReferenceEquals(n, deepest));
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static IEnumerable<GherkinSymbolNode> Flatten(IReadOnlyList<GherkinSymbolNode> nodes)
    {
        foreach (var node in nodes)
        {
            yield return node;
            foreach (var child in Flatten(node.Children))
                yield return child;
        }
    }

    private static GherkinSymbolNode? FindDeepestContainer(IReadOnlyList<GherkinSymbolNode> nodes, int caretLine)
    {
        GherkinSymbolNode? best = null;
        foreach (var node in nodes)
        {
            if (!Contains(node.Range, caretLine))
                continue;

            if (GherkinSymbolKinds.IsContainer(node.Kind))
                best = node;

            var deeper = FindDeepestContainer(node.Children, caretLine);
            if (deeper is not null)
                best = deeper;

            // Ranges don't overlap between siblings, so the first containing node is the only one.
            break;
        }
        return best;
    }

    private static bool Contains(GherkinSymbolRange range, int line) =>
        line >= range.Start.Line && line <= range.End.Line;
}
