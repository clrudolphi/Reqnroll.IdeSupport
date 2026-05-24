using Reqnroll.IdeSupport.LSP.Core.Document;

namespace Reqnroll.IdeSupport.LSP.Core.Editor.Services.Parsing.GherkinDocuments;

public interface IDeveroomTagParser
{
    IReadOnlyCollection<DeveroomTag> Parse(IGherkinTextSnapshot fileSnapshot);
}
