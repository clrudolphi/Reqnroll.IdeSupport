#nullable disable

using Gherkin.Ast;

namespace Reqnroll.IdeSupport.LSPServer.Core.Editor.Services.Parsing.GherkinDocuments;

public class SingleScenario : Scenario
{
    public SingleScenario(Tag[] tags, Location location, string keyword, string name, string description, Step[] steps,
        Examples[] examples = null) : base(tags, location, keyword, name, description, steps,
        examples ?? new Examples[0])
    {
    }
}
