
namespace Reqnroll.IdeSupport.LSP.Core.Parsing.Gherkin;

/// <summary>
/// A parser error raised by Reqnroll's own semantic checks (duplicate scenario/example names,
/// missing examples) rather than by the underlying Gherkin grammar parser.
/// </summary>
public class SemanticParserException : ParserException
{
    /// <summary>Creates a semantic parser exception with no specific source location.</summary>
    public SemanticParserException(string message) : base(message)
    {
    }

    /// <summary>Creates a semantic parser exception pointing at the given source location.</summary>
    public SemanticParserException(string message, Location location) : base(message, location)
    {
    }
}
