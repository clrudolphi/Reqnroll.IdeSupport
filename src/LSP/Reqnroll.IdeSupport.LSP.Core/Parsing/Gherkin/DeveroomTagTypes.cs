namespace Reqnroll.IdeSupport.LSP.Core.Parsing.Gherkin;

/// <summary>
/// String constants identifying the kind of a <c>DeveroomTag</c> node produced while walking a
/// parsed feature document (e.g. by <c>DeveroomTagParser</c>) — used to classify tags for
/// semantic tokens, diagnostics, and binding-match lookup.
/// </summary>
public static class DeveroomTagTypes
{
    /// <summary>Gets or sets the feature block.</summary>
    public const string FeatureBlock = nameof(FeatureBlock);
    /// <summary>Gets or sets the rule block.</summary>
    public const string RuleBlock = nameof(RuleBlock);
    /// <summary>Gets or sets the scenario definition block.</summary>
    public const string ScenarioDefinitionBlock = nameof(ScenarioDefinitionBlock);
    /// <summary>Gets or sets the scenario hook reference.</summary>
    public const string ScenarioHookReference = nameof(ScenarioHookReference);
    /// <summary>Gets or sets the step block.</summary>
    public const string StepBlock = nameof(StepBlock);
    /// <summary>Gets or sets the examples block.</summary>
    public const string ExamplesBlock = nameof(ExamplesBlock);
    /// <summary>Gets or sets the step keyword.</summary>
    public const string StepKeyword = nameof(StepKeyword);
    /// <summary>Gets or sets the definition line keyword.</summary>
    public const string DefinitionLineKeyword = nameof(DefinitionLineKeyword);
    /// <summary>Gets or sets the undefined step.</summary>
    public const string UndefinedStep = nameof(UndefinedStep);
    /// <summary>Gets or sets the defined step.</summary>
    public const string DefinedStep = nameof(DefinedStep);
    /// <summary>Gets or sets the ambiguous step.</summary>
    public const string AmbiguousStep = nameof(AmbiguousStep);
    /// <summary>Gets or sets the step parameter.</summary>
    public const string StepParameter = nameof(StepParameter);
    /// <summary>Gets or sets the scenario outline placeholder.</summary>
    public const string ScenarioOutlinePlaceholder = nameof(ScenarioOutlinePlaceholder);
    /// <summary>Gets or sets the binding error.</summary>
    public const string BindingError = nameof(BindingError);
    /// <summary>Gets or sets the data table.</summary>
    public const string DataTable = nameof(DataTable);
    /// <summary>Gets or sets the tag.</summary>
    public const string Tag = nameof(Tag);
    /// <summary>Gets or sets the description.</summary>
    public const string Description = nameof(Description);
    /// <summary>Gets or sets the comment.</summary>
    public const string Comment = nameof(Comment);
    /// <summary>Gets or sets the doc string.</summary>
    public const string DocString = nameof(DocString);
    /// <summary>Gets or sets the parser error.</summary>
    public const string ParserError = nameof(ParserError);
    /// <summary>Gets or sets the document.</summary>
    public const string Document = nameof(Document);
    /// <summary>Gets or sets the data table header.</summary>
    public const string DataTableHeader = nameof(DataTableHeader);
}
