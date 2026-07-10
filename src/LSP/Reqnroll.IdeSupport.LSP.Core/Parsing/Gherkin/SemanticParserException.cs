
namespace Reqnroll.IdeSupport.LSP.Core.Parsing.Gherkin;

public class SemanticParserException : ParserException
{
    public SemanticParserException(string message) : base(message)
    {
    }

    public SemanticParserException(string message, Location location) : base(message, location)
    {
    }
}
