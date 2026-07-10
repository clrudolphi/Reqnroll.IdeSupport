using Reqnroll.IdeSupport.LSP.Core.Parsing.Gherkin;

namespace Reqnroll.IdeSupport.LSP.Core.Completions;

/// <summary>
/// Base type for the two mutually exclusive completion contexts resolved from a cursor position
/// in a .feature file. Use a <c>switch</c> expression on the concrete subtype to dispatch.
/// </summary>
public abstract class CompletionContext { }

/// <summary>
/// Cursor is on a keyword position (start of line, or before any step text).
/// <see cref="ExpectedTokens"/> is empty when the document could not be parsed — callers should
/// fall back to <see cref="ICompletionService.GetDefaultKeywordCompletions"/> in that case.
/// </summary>
public sealed class KeywordCompletionContext : CompletionContext
{
    /// <summary>Gets or sets the dialect.</summary>
    public GherkinDialect Dialect        { get; }
    /// <summary>Gets or sets the expected tokens.</summary>
    public TokenType[]    ExpectedTokens { get; }

    /// <summary>Initializes a new instance of the <see cref="KeywordCompletionContext"/> class.</summary>
    public KeywordCompletionContext(GherkinDialect dialect, TokenType[] expectedTokens)
    {
        Dialect        = dialect;
        ExpectedTokens = expectedTokens;
    }
}

/// <summary>
/// Cursor is on a step line, past the step keyword — trigger step-definition sample completion.
/// </summary>
public sealed class StepCompletionContext : CompletionContext
{
    /// <summary>The Gherkin step the cursor is on, used to filter completions by <c>ScenarioBlock</c>.</summary>
    public DeveroomGherkinStep Step               { get; }

    /// <summary>Text the user has typed after the keyword and its trailing space.</summary>
    public string              TypedAfterKeyword  { get; }

    /// <summary>Zero-based column index of the first character after the keyword (the start of the step text).</summary>
    public int                 StepTextStartColumn { get; }

    /// <summary>Initializes a new instance of the <see cref="StepCompletionContext"/> class.</summary>
    public StepCompletionContext(
        DeveroomGherkinStep step,
        string              typedAfterKeyword,
        int                 stepTextStartColumn)
    {
        Step                = step;
        TypedAfterKeyword   = typedAfterKeyword;
        StepTextStartColumn = stepTextStartColumn;
    }
}
