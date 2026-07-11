using Cucumber.TagExpressions;
using Gherkin.Ast;

namespace Reqnroll.IdeSupport.LSP.Core.TagExpressions;

/// <summary>TagExpressionExtensions</summary>
public static class TagExpressionExtensions
{
    /// <summary>Evaluates the tag expression against the given tag names, or returns <paramref name="defaultValue"/> when the expression is <see langword="null"/>.</summary>
    public static bool EvaluateWithDefault(this ITagExpression tagExpression, IEnumerable<string> tags,
        bool defaultValue) => tagExpression?.Evaluate(tags) ?? defaultValue;

    /// <summary>Evaluates the tag expression against the names of the given Gherkin <see cref="Tag"/> objects, or returns <paramref name="defaultValue"/> when the expression is <see langword="null"/>.</summary>
    public static bool EvaluateWithDefault(this ITagExpression tagExpression, IEnumerable<Tag> tags, bool defaultValue)
    {
        return tagExpression?.Evaluate(tags.Select(t => t.Name)) ?? defaultValue;
    }
}
