using Reqnroll.IdeSupport.LSP.Core.Parsing.Gherkin;

namespace Reqnroll.IdeSupport.LSP.Core.DocumentOutline;

public interface IGherkinDocumentSymbolService
{
    IReadOnlyList<GherkinDocumentSymbol> BuildSymbols(IReadOnlyCollection<DeveroomTag> tags);
}
