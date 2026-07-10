#nullable disable

using Gherkin.Ast;

namespace Reqnroll.IdeSupport.LSP.Core.Parsing.Gherkin;

/// <summary>A plain (non-outline) Gherkin Scenario node.</summary>
public class SingleScenario : Scenario
{
    /// <summary>Creates a single scenario node from its parsed parts.</summary>
    public SingleScenario(IEnumerable<Tag> tags, Location location, string keyword, string name, string description, IEnumerable<Step> steps,
        IEnumerable<Examples> examples = null) : base(tags, location, keyword, name, description, steps,
        examples ?? new Examples[0])
    {
    }
}
