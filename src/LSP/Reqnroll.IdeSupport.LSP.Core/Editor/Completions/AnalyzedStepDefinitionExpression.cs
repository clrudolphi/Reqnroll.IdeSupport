using System.Collections.Immutable;

namespace Reqnroll.IdeSupport.LSP.Core.Editor.Completions;

// ── Part hierarchy ────────────────────────────────────────────────────────────

public abstract record AnalyzedStepDefinitionExpressionPart
{
    public abstract string ExpressionText { get; }
}

/// <summary>Plain text with no regex operators — can be safely displayed after unescaping.</summary>
public record AnalyzedStepDefinitionExpressionSimpleTextPart : AnalyzedStepDefinitionExpressionPart
{
    public AnalyzedStepDefinitionExpressionSimpleTextPart(string text, string unescapedText)
    {
        Text         = text;
        UnescapedText = unescapedText ?? text;
    }

    public string Text          { get; }
    public string UnescapedText { get; }
    public override string ExpressionText => Text;
}

/// <summary>Plain text that contains regex operators; the sampler falls back to the raw regex when any text part has this type.</summary>
public record AnalyzedStepDefinitionExpressionWithOperatorsTextPart : AnalyzedStepDefinitionExpressionPart
{
    public AnalyzedStepDefinitionExpressionWithOperatorsTextPart(string text) => Text = text;
    public string Text { get; }
    public override string ExpressionText => Text;
}

/// <summary>A capturing group that corresponds to a step-definition parameter.</summary>
public record AnalyzedStepDefinitionExpressionParameterPart : AnalyzedStepDefinitionExpressionPart
{
    public AnalyzedStepDefinitionExpressionParameterPart(string parameterExpression)
        => ParameterExpression = parameterExpression;

    public string ParameterExpression { get; }
    public override string ExpressionText => ParameterExpression;
}

// ── Analyzed expression ───────────────────────────────────────────────────────

/// <summary>
/// Result of parsing a step-definition regex expression into alternating text/parameter parts.
/// Odd-indexed parts (0, 2, 4, …) are text; even-indexed parts (1, 3, 5, …) are parameters.
/// </summary>
public sealed class AnalyzedStepDefinitionExpression
{
    public AnalyzedStepDefinitionExpression(ImmutableArray<AnalyzedStepDefinitionExpressionPart> parts)
        => Parts = parts;

    public ImmutableArray<AnalyzedStepDefinitionExpressionPart> Parts { get; }

    /// <summary>
    /// <see langword="true"/> when every text-position part (0, 2, 4, …) is a
    /// <see cref="AnalyzedStepDefinitionExpressionSimpleTextPart"/>, i.e. no regex operators
    /// appear outside capturing groups.
    /// </summary>
    public bool ContainsOnlySimpleText =>
        Parts.OfType<AnalyzedStepDefinitionExpressionSimpleTextPart>().Count() == Parts.Length / 2 + 1;

    public IEnumerable<AnalyzedStepDefinitionExpressionParameterPart> ParameterParts =>
        Parts.OfType<AnalyzedStepDefinitionExpressionParameterPart>();
}
