namespace Reqnroll.IdeSupport.Common.Configuration;

/// <summary>SnippetExpressionStyle</summary>
public enum SnippetExpressionStyle
{
    /// <summary>Gets or sets the regular expression.</summary>
    RegularExpression,
    /// <summary>Gets or sets the cucumber expression.</summary>
    CucumberExpression,
    /// <summary>Gets or sets the async regular expression.</summary>
    AsyncRegularExpression,
    /// <summary>Gets or sets the async cucumber expression.</summary>
    AsyncCucumberExpression
}

/// <summary>SnippetExpressionStyleExtensions</summary>
public static class SnippetExpressionStyleExtensions
{
    /// <summary>Gets or sets the is async.</summary>
    public static bool IsAsync(this SnippetExpressionStyle style)
    {
        if (style == SnippetExpressionStyle.AsyncRegularExpression
            || style == SnippetExpressionStyle.AsyncCucumberExpression)
            return true;
        return false;
    }

    /// <summary>Gets or sets the is cucumber.</summary>
    public static bool IsCucumber(this SnippetExpressionStyle style)
    {
        if (style == SnippetExpressionStyle.CucumberExpression
            || style == SnippetExpressionStyle.AsyncCucumberExpression)
            return true;
        return false;
    }
}