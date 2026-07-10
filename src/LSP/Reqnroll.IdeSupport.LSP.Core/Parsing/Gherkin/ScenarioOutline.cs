using Gherkin.Ast;

namespace Reqnroll.IdeSupport.LSP.Core.Parsing.Gherkin;

/// <summary>A Gherkin Scenario Outline node: a templated scenario with one or more Examples tables.</summary>
public class ScenarioOutline : Scenario
{
    /// <summary>Creates a scenario outline node from its parsed parts.</summary>
    public ScenarioOutline(IEnumerable<Tag> tags, Location location, string keyword, string name, string description, IEnumerable<Step> steps,
        IEnumerable<Examples> examples) : base(tags, location, keyword, name, description, steps, examples)
    {
    }
}
