using Reqnroll.IdeSupport.LSP.Core.Gherkin.Parsing;

namespace Reqnroll.IdeSupport.LSP.Core.DocumentOutline;

public interface IGherkinDocumentSymbolService
{
    IReadOnlyList<GherkinDocumentSymbol> BuildSymbols(IReadOnlyCollection<DeveroomTag> tags);
}
