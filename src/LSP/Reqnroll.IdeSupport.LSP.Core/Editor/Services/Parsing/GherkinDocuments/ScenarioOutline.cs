using Gherkin.Ast;

namespace Reqnroll.IdeSupport.LSP.Core.Editor.Services.Parsing.GherkinDocuments;

public class ScenarioOutline : Scenario
{
    public ScenarioOutline(IEnumerable<Tag> tags, Location location, string keyword, string name, string description, IEnumerable<Step> steps,
        IEnumerable<Examples> examples) : base(tags, location, keyword, name, description, steps, examples)
    {
    }
}
