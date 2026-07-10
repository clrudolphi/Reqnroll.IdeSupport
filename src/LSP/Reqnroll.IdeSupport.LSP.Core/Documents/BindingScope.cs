using Cucumber.TagExpressions;

namespace Reqnroll.IdeSupport.LSP.Core.Documents;

/// <summary>BindingScope</summary>
public class BindingScope
{
    /// <summary>Gets or sets the tag.</summary>
    public ITagExpression? Tag { get; set; }
    /// <summary>Gets or sets the feature title.</summary>
    public string? FeatureTitle { get; set; }
    /// <summary>Gets or sets the scenario title.</summary>
    public string? ScenarioTitle { get; set; }
    /// <summary>Gets or sets the error.</summary>
    public string? Error { get; set; }

    /// <summary>Gets or sets the is valid.</summary>
    public bool IsValid => Error == null;

    /// <summary>Gets or sets the to string.</summary>
    public override string ToString()
    {
        var result = Tag?.ToString() ?? "";
        if (FeatureTitle != null)
        {
            result = result.Length > 0 ? result + ", " : result;
            result = $"{result}Feature='{FeatureTitle}'";
        }
        if (ScenarioTitle != null)
        {
            result = result.Length > 0 ? result + ", " : result;
            result = $"{result}Scenario='{ScenarioTitle}'";
        }
        if (Error != null)
        {
            result = result.Length > 0 ? result + ", " : result;
            result = $"{result}Error='{Error}'";
        }
        return result;
    }
}
