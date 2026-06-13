using AwesomeAssertions;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Reqnroll;
using Reqnroll.IdeSupport.LSP.Server.Specs.Support;

namespace Reqnroll.IdeSupport.LSP.Server.Specs.StepDefinitions;

[Binding]
public sealed class DocumentOutlineSteps
{
    private readonly LspScenarioContext _ctx;

    public DocumentOutlineSteps(LspScenarioContext ctx) => _ctx = ctx;

    // ── When ──────────────────────────────────────────────────────────────────

    [When(@"the document outline is requested for ""(.*)""")]
    public async Task WhenTheDocumentOutlineIsRequestedFor(string fileName)
    {
        await _ctx.EnsureStartedAsync().ConfigureAwait(false);
        var uri = _ctx.UriFor(fileName);
        _ctx.LastDocumentSymbols = await _ctx.Harness.Client
            .RequestDocumentSymbolAsync(uri)
            .ConfigureAwait(false);
    }

    // ── Then ──────────────────────────────────────────────────────────────────

    [Then(@"the outline is empty")]
    public void ThenTheOutlineIsEmpty()
    {
        var count = _ctx.LastDocumentSymbols?.Count() ?? 0;
        count.Should().Be(0, "a non-.feature file should yield no document symbols");
    }

    [Then(@"the outline contains (\d+) top-level symbol[s]?")]
    public void ThenTheOutlineContainsTopLevelSymbols(int expected)
    {
        TopLevelSymbols().Should().HaveCount(expected);
    }

    [Then(@"the first top-level symbol has name ""(.*)"" and kind ""(.*)""")]
    public void ThenTheFirstTopLevelSymbolHasNameAndKind(string name, string kind)
    {
        var first = TopLevelSymbols().First();
        first.Name.Should().Be(name);
        first.Kind.Should().Be(ParseKind(kind));
    }

    [Then(@"the first child of ""(.*)"" has name ""(.*)"" and kind ""(.*)""")]
    public void ThenTheFirstChildHasNameAndKind(string parentName, string childName, string kind)
    {
        var parent = FindSymbolByName(TopLevelSymbols(), parentName)
                  ?? FindSymbolRecursive(AllDocumentSymbols(), parentName);
        parent.Should().NotBeNull($"a symbol named '{parentName}' should exist");
        var first = (parent!.Children ?? Enumerable.Empty<DocumentSymbol>()).First();
        first.Name.Should().Be(childName);
        first.Kind.Should().Be(ParseKind(kind));
    }

    [Then(@"the children of ""(.*)"" contain a symbol named ""(.*)"" with kind ""(.*)""")]
    public void ThenTheChildrenContainSymbol(string parentName, string childName, string kind)
    {
        var parent = FindSymbolByName(TopLevelSymbols(), parentName)
                  ?? FindSymbolRecursive(AllDocumentSymbols(), parentName);
        parent.Should().NotBeNull($"a symbol named '{parentName}' should exist");
        var children = parent!.Children ?? Enumerable.Empty<DocumentSymbol>();
        children.Should().Contain(
            c => c.Name == childName && c.Kind == ParseKind(kind),
            $"children of '{parentName}' should include '{childName}' with kind '{kind}'");
    }

    [Then(@"""(.*)"" has (\d+) children")]
    public void ThenSymbolHasChildren(string parentName, int expectedCount)
    {
        var parent = FindSymbolRecursive(AllDocumentSymbols(), parentName);
        parent.Should().NotBeNull($"a symbol named '{parentName}' should exist");
        var children = parent!.Children ?? Enumerable.Empty<DocumentSymbol>();
        children.Should().HaveCount(expectedCount);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private IEnumerable<DocumentSymbol> TopLevelSymbols()
        => AllDocumentSymbols();

    private IEnumerable<DocumentSymbol> AllDocumentSymbols()
    {
        if (_ctx.LastDocumentSymbols is null)
            return Enumerable.Empty<DocumentSymbol>();

        return _ctx.LastDocumentSymbols
            .Where(x => x.IsDocumentSymbol)
            .Select(x => x.DocumentSymbol!);
    }

    private static DocumentSymbol? FindSymbolByName(
        IEnumerable<DocumentSymbol> symbols, string name)
        => symbols.FirstOrDefault(s => s.Name == name);

    private static DocumentSymbol? FindSymbolRecursive(
        IEnumerable<DocumentSymbol> symbols, string name)
    {
        foreach (var s in symbols)
        {
            if (s.Name == name) return s;
            if (s.Children is not null)
            {
                var found = FindSymbolRecursive(s.Children, name);
                if (found is not null) return found;
            }
        }
        return null;
    }

    private static SymbolKind ParseKind(string kind) => kind switch
    {
        "Module"      => SymbolKind.Module,
        "Method"      => SymbolKind.Method,
        "Constructor" => SymbolKind.Constructor,
        "Namespace"   => SymbolKind.Namespace,
        "Field"       => SymbolKind.Field,
        "Array"       => SymbolKind.Array,
        _ => throw new ArgumentException($"Unknown symbol kind: '{kind}'")
    };
}
