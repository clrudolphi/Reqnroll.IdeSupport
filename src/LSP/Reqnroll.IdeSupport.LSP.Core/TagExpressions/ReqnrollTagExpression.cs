using Cucumber.TagExpressions;

namespace Reqnroll.IdeSupport.LSP.Core.TagExpressions;

/// <summary>ReqnrollTagExpression</summary>
public class ReqnrollTagExpression : ITagExpression
{
    /// <summary>Initializes a new instance of the <see cref="ReqnrollTagExpression"/> class.</summary>
    public ReqnrollTagExpression(ITagExpression inner, string tagExpressionText)
    {
        TagExpressionText = tagExpressionText;
        _inner = inner;
    }
    /// <summary>Gets or sets the tag expression text.</summary>
    public string TagExpressionText { get; }

    private ITagExpression _inner;

    /// <summary>Gets or sets the to string.</summary>
    public override string ToString()
    {
        return _inner.ToString();
    }

    /// <summary>Gets or sets the evaluate.</summary>
    public virtual bool Evaluate(IEnumerable<string> inputs)
    {
        return _inner.Evaluate(inputs);
    }
}
