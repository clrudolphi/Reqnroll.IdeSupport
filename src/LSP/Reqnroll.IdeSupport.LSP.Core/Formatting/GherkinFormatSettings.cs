using Reqnroll.IdeSupport.Common.Configuration;
using Reqnroll.IdeSupport.Common.ProjectSystem.Configuration;

namespace Reqnroll.IdeSupport.LSP.Core.Formatting;

/// <summary>
/// Resolved formatting settings for a single document format pass.
/// </summary>
public class GherkinFormatSettings
{
    /// <summary>Gets or sets the configuration.</summary>
    public GherkinFormatConfiguration Configuration { get; set; } = new();

    /// <summary>Gets or sets the indent.</summary>
    public string Indent { get; set; } = "    ";

    /// <summary>Gets or sets the feature children indent level.</summary>
    public int FeatureChildrenIndentLevel => Configuration.IndentFeatureChildren ? 1 : 0;
    /// <summary>Gets or sets the rule children indent level within rule.</summary>
    public int RuleChildrenIndentLevelWithinRule => Configuration.IndentRuleChildren ? 1 : 0;
    /// <summary>Gets or sets the step indent level within step container.</summary>
    public int StepIndentLevelWithinStepContainer => Configuration.IndentSteps ? 1 : 0;
    /// <summary>Gets or sets the and step indent level within steps.</summary>
    public int AndStepIndentLevelWithinSteps => Configuration.IndentAndSteps ? 1 : 0;
    /// <summary>Gets or sets the data table indent level within step.</summary>
    public int DataTableIndentLevelWithinStep => Configuration.IndentDataTable ? 1 : 0;
    /// <summary>Gets or sets the doc string indent level within step.</summary>
    public int DocStringIndentLevelWithinStep => Configuration.IndentDocString ? 1 : 0;
    /// <summary>Gets or sets the examples block indent level within scenario outline.</summary>
    public int ExamplesBlockIndentLevelWithinScenarioOutline => Configuration.IndentExamples ? 1 : 0;
    /// <summary>Gets or sets the examples table indent level within examples block.</summary>
    public int ExamplesTableIndentLevelWithinExamplesBlock => Configuration.IndentExamplesTable ? 1 : 0;
    /// <summary>Gets or sets the table cell padding.</summary>
    public string TableCellPadding => new(' ', Configuration.TableCellPaddingSize);
    /// <summary>Gets or sets the right align numeric table cells.</summary>
    public bool RightAlignNumericTableCells => Configuration.TableCellRightAlignNumericContent;

    /// <summary>
    /// Builds format settings from LSP <c>FormattingOptions</c>, applying editorconfig overrides
    /// and any Reqnroll-specific configuration from <paramref name="configuration"/>.
    /// </summary>
    public static GherkinFormatSettings FromLspOptions(
        int tabSize, bool insertSpaces,
        IEditorConfigOptionsProvider editorConfigOptionsProvider,
        string filePath,
        DeveroomConfiguration? configuration)
    {
        var gherkinFormatConfig = configuration?.Editor?.GherkinFormat?.Clone() ?? new GherkinFormatConfiguration();

        var editorConfigOptions = editorConfigOptionsProvider.GetEditorConfigOptionsByPath(filePath);
        editorConfigOptions.UpdateFromEditorConfig(gherkinFormatConfig);

        var indent = insertSpaces ? new string(' ', tabSize) : "\t";

        return new GherkinFormatSettings
        {
            Indent = indent,
            Configuration = gherkinFormatConfig
        };
    }
}
