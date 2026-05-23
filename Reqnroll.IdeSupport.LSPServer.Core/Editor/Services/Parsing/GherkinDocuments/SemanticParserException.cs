
namespace Reqnroll.IdeSupport.LSPServer.Core.Editor.Services.Parsing.GherkinDocuments;

public class SemanticParserException : ParserException
{
    public SemanticParserException(string message) : base(message)
    {
    }

    public SemanticParserException(string message, Location location) : base(message, location)
    {
    }
}
