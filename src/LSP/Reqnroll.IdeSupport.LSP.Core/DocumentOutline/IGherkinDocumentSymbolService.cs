using Reqnroll.IdeSupport.LSP.Core.Editor.Services.Parsing.GherkinDocuments;

namespace Reqnroll.IdeSupport.LSP.Core.DocumentOutline;

public interface IGherkinDocumentSymbolService
{
    IReadOnlyList<GherkinDocumentSymbol> BuildSymbols(IReadOnlyCollection<DeveroomTag> tags);
}
