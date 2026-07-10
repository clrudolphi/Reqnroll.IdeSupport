

using Cucumber.TagExpressions;

namespace Reqnroll.IdeSupport.LSP.Core.TagExpressions;
/// <summary>InvalidTagExpression</summary>
public class InvalidTagExpression : ReqnrollTagExpression, ITagExpression
{
    /// <summary>Gets or sets the message.</summary>
    public string Message { get; }
    /// <summary>Initializes a new instance of the <see cref="InvalidTagExpression"/> class.</summary>
    public InvalidTagExpression(ITagExpression? expression, string originalTagExpression, string message) : base(expression!, originalTagExpression)
    {
        Message = message;
    }
    /// <summary>Gets or sets the evaluate.</summary>
    public override bool Evaluate(IEnumerable<string> tags)
    {
        throw new InvalidOperationException("Cannot evaluate an invalid tag expression: " + Message);
    }
    /// <summary>Gets or sets the to string.</summary>
    public override string ToString()
    {
        return "Invalid Tag Expression: " + Message;
    }
}
