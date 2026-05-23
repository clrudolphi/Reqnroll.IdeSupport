using Reqnroll.IDESupport.LSPServer.Core.Document;

namespace Reqnroll.IdeSupport.LSPServer.Core.Editor.Services.Parsing.GherkinDocuments;

public interface IDeveroomTagParser
{
    IReadOnlyCollection<DeveroomTag> Parse(IGherkinTextSnapshot fileSnapshot);
}
