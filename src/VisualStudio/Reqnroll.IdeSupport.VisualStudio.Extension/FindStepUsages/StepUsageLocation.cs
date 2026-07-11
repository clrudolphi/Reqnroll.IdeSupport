namespace Reqnroll.IdeSupport.VisualStudio.Extension.FindStepUsages;

/// <summary>
/// A single step-usage location returned by <see cref="FindStepUsagesService"/>.
/// All positions are 0-based (LSP convention).
/// </summary>
internal sealed class StepUsageLocation
{
    /// <summary>Creates a step-usage location from server-supplied coordinates and optional display metadata.</summary>
    public StepUsageLocation(
        string  fileUri,
        int     startLine,
        int     startChar,
        int     endLine,
        int     endChar,
        string? stepText     = null,
        string? keyword      = null,
        string? scenarioName = null,
        string? projectName  = null)
    {
        FileUri      = fileUri;
        StartLine    = startLine;
        StartChar    = startChar;
        EndLine      = endLine;
        EndChar      = endChar;
        StepText     = stepText;
        Keyword      = keyword;
        ScenarioName = scenarioName;
        ProjectName  = projectName;
    }

    /// <summary>The document URI of the feature file containing the step.</summary>
    public string  FileUri   { get; }
    /// <summary>0-based start line of the step usage.</summary>
    public int     StartLine { get; }
    /// <summary>0-based start character of the step usage.</summary>
    public int     StartChar { get; }
    /// <summary>0-based end line of the step usage.</summary>
    public int     EndLine   { get; }
    /// <summary>0-based end character of the step usage.</summary>
    public int     EndChar   { get; }

    /// <summary>
    /// The trimmed step text as supplied by the server (e.g. <c>"the first number is 50"</c>).
    /// <see langword="null"/> when the server did not include it; the client falls back to
    /// reading the feature file from disk in that case.
    /// </summary>
    public string? StepText  { get; }

    /// <summary>Trimmed Gherkin keyword (e.g. <c>"Given"</c>, <c>"When"</c>, <c>"Then"</c>).</summary>
    public string? Keyword   { get; }

    /// <summary>Enclosing scenario or scenario-outline name; <see langword="null"/> for Background steps.</summary>
    public string? ScenarioName { get; }

    /// <summary>Short project name derived from the owning .csproj file name (e.g. <c>"Minimal"</c>).</summary>
    public string? ProjectName  { get; }
}
