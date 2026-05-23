using Gherkin.Ast;

namespace Reqnroll.IdeSupport.LSPServer.Core.Editor.Services.Parsing.GherkinDocuments;

public class ScenarioOutline : Scenario
{
    public ScenarioOutline(Tag[] tags, Location location, string keyword, string name, string description, Step[] steps,
        Examples[] examples) : base(tags, location, keyword, name, description, steps, examples)
    {
    }
}
