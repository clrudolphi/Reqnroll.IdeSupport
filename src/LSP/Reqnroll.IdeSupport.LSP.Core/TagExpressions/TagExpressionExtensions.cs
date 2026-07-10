using Cucumber.TagExpressions;
using Gherkin.Ast;

namespace Reqnroll.IdeSupport.LSP.Core.TagExpressions;

/// <summary>TagExpressionExtensions</summary>
public static class TagExpressionExtensions
{
    /// <summary>Gets or sets the evaluate with default.</summary>
    public static bool EvaluateWithDefault(this ITagExpression tagExpression, IEnumerable<string> tags,
        bool defaultValue) => tagExpression?.Evaluate(tags) ?? defaultValue;

    /// <summary>Gets or sets the evaluate with default.</summary>
    public static bool EvaluateWithDefault(this ITagExpression tagExpression, IEnumerable<Tag> tags, bool defaultValue)
    {
        return tagExpression?.Evaluate(tags.Select(t => t.Name)) ?? defaultValue;
    }
}
