using Gherkin.Ast;

namespace Reqnroll.IdeSupport.LSP.Core.Parsing.Gherkin;

/// <summary>
/// A single <c>&lt;placeholder&gt;</c> match found within a Scenario Outline step's text, e.g.
/// the <c>&lt;name&gt;</c> in "I enter &lt;name&gt;".
/// </summary>
public struct MatchedScenarioOutlinePlaceholder
{
    private readonly Match _match;

    /// <summary>The character index where the placeholder (including angle brackets) starts.</summary>
    public int Index => _match.Index;
    /// <summary>The length, in characters, of the placeholder including its angle brackets.</summary>
    public int Length => _match.Length;
    /// <summary>The full matched text, e.g. <c>"&lt;name&gt;"</c>.</summary>
    public string Value => _match.Value;
    /// <summary>The placeholder name without angle brackets, e.g. <c>"name"</c>.</summary>
    public string Name => _match.Groups["name"].Value;

    /// <summary>Wraps a regex match as a scenario outline placeholder.</summary>
    public MatchedScenarioOutlinePlaceholder(Match match)
    {
        _match = match;
    }

    private static readonly Regex ScenarioOutlineParamRe = new(@"\<(?<name>[^\>]+)\>");

    /// <summary>Finds every <c>&lt;placeholder&gt;</c> in a step's text.</summary>
    public static IEnumerable<MatchedScenarioOutlinePlaceholder> MatchScenarioOutlinePlaceholders(Step step)
    {
        return ScenarioOutlineParamRe.Matches(step.Text).Cast<Match>()
            .Select(m => new MatchedScenarioOutlinePlaceholder(m));
    }

    /// <summary>Replaces every <c>&lt;placeholder&gt;</c> in a step's text using <paramref name="replace"/>.</summary>
    public static string ReplaceScenarioOutlinePlaceholders(Step step,
        Func<MatchedScenarioOutlinePlaceholder, string> replace)
    {
        return ScenarioOutlineParamRe.Replace(step.Text, m => replace(new MatchedScenarioOutlinePlaceholder(m)));
    }
}
