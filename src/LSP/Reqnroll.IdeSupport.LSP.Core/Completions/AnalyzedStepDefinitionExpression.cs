using System.Collections.Immutable;

namespace Reqnroll.IdeSupport.LSP.Core.Completions;

// ── Part hierarchy ────────────────────────────────────────────────────────────

/// <summary>AnalyzedStepDefinitionExpressionPart</summary>
public abstract record AnalyzedStepDefinitionExpressionPart
{
    /// <summary>Gets or sets the expression text.</summary>
    public abstract string ExpressionText { get; }
}

/// <summary>Plain text with no regex operators — can be safely displayed after unescaping.</summary>
public record AnalyzedStepDefinitionExpressionSimpleTextPart : AnalyzedStepDefinitionExpressionPart
{
    /// <summary>Initializes a new instance of the <see cref="AnalyzedStepDefinitionExpressionSimpleTextPart"/> class.</summary>
    public AnalyzedStepDefinitionExpressionSimpleTextPart(string text, string unescapedText)
    {
        Text         = text;
        UnescapedText = unescapedText ?? text;
    }

    /// <summary>Gets or sets the text.</summary>
    public string Text          { get; }
    /// <summary>Gets or sets the unescaped text.</summary>
    public string UnescapedText { get; }
    /// <summary>Gets or sets the expression text.</summary>
    public override string ExpressionText => Text;
}

/// <summary>Plain text that contains regex operators; the sampler falls back to the raw regex when any text part has this type.</summary>
public record AnalyzedStepDefinitionExpressionWithOperatorsTextPart : AnalyzedStepDefinitionExpressionPart
{
    /// <summary>Initializes a new instance of the <see cref="AnalyzedStepDefinitionExpressionWithOperatorsTextPart"/> class.</summary>
    public AnalyzedStepDefinitionExpressionWithOperatorsTextPart(string text) => Text = text;
    /// <summary>Gets or sets the text.</summary>
    public string Text { get; }
    /// <summary>Gets or sets the expression text.</summary>
    public override string ExpressionText => Text;
}

/// <summary>A capturing group that corresponds to a step-definition parameter.</summary>
public record AnalyzedStepDefinitionExpressionParameterPart : AnalyzedStepDefinitionExpressionPart
{
    /// <summary>Initializes a new instance of the <see cref="AnalyzedStepDefinitionExpressionParameterPart"/> class.</summary>
    public AnalyzedStepDefinitionExpressionParameterPart(string parameterExpression)
        => ParameterExpression = parameterExpression;

    /// <summary>Gets or sets the parameter expression.</summary>
    public string ParameterExpression { get; }
    /// <summary>Gets or sets the expression text.</summary>
    public override string ExpressionText => ParameterExpression;
}

// ── Analyzed expression ───────────────────────────────────────────────────────

/// <summary>
/// Result of parsing a step-definition regex expression into alternating text/parameter parts.
/// Odd-indexed parts (0, 2, 4, …) are text; even-indexed parts (1, 3, 5, …) are parameters.
/// </summary>
public sealed class AnalyzedStepDefinitionExpression
{
    /// <summary>Initializes a new instance of the <see cref="AnalyzedStepDefinitionExpression"/> class.</summary>
    public AnalyzedStepDefinitionExpression(ImmutableArray<AnalyzedStepDefinitionExpressionPart> parts)
        => Parts = parts;

    /// <summary>Gets or sets the parts.</summary>
    public ImmutableArray<AnalyzedStepDefinitionExpressionPart> Parts { get; }

    /// <summary>
    /// <see langword="true"/> when every text-position part (0, 2, 4, …) is a
    /// <see cref="AnalyzedStepDefinitionExpressionSimpleTextPart"/>, i.e. no regex operators
    /// appear outside capturing groups.
    /// </summary>
    public bool ContainsOnlySimpleText =>
        Parts.OfType<AnalyzedStepDefinitionExpressionSimpleTextPart>().Count() == Parts.Length / 2 + 1;

    /// <summary>Gets or sets the parameter parts.</summary>
    public IEnumerable<AnalyzedStepDefinitionExpressionParameterPart> ParameterParts =>
        Parts.OfType<AnalyzedStepDefinitionExpressionParameterPart>();
}
