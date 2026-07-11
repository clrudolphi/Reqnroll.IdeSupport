#nullable disable

using Gherkin.Ast;

namespace Reqnroll.IdeSupport.LSP.Core.Matching;

/// <summary>
/// Describes how a step's text (and optional table/doc-string argument) mapped onto the
/// parameters of a matched step definition.
/// </summary>
public class ParameterMatch
{
    /// <summary>Sentinel used for a step definition match that has no parameters to report.</summary>
    public static readonly ParameterMatch NotMatch = new(Array.Empty<MatchedStepTextParameter>(), null,
        Array.Empty<string>());

    /// <summary>Creates a parameter match result.</summary>
    public ParameterMatch(MatchedStepTextParameter[] stepTextParameters, StepArgument stepArgument,
        string[] parameterTypes, string error = null)
    {
        StepTextParameters = stepTextParameters ?? throw new ArgumentNullException(nameof(stepTextParameters));
        StepArgument = stepArgument;
        ParameterTypes = parameterTypes ?? throw new ArgumentNullException(nameof(parameterTypes));
        Error = error;
    }

    /// <summary>The spans within the step text that were captured as parameters.</summary>
    public MatchedStepTextParameter[] StepTextParameters { get; }
    /// <summary>The step's data table or doc string argument, if any.</summary>
    public StepArgument StepArgument { get; }
    /// <summary>The resolved .NET parameter types for the matched step definition, in order.</summary>
    public string[] ParameterTypes { get; }
    /// <summary>An error message describing why the match failed or is incomplete, if any.</summary>
    public string Error { get; }

    /// <summary>True when <see cref="Error"/> is set.</summary>
    public bool HasError => Error != null;
    /// <summary>True when <see cref="StepArgument"/> is a data table.</summary>
    public bool MatchedDataTable => StepArgument is DataTable;
    /// <summary>The parameter type bound to the data table argument, when <see cref="MatchedDataTable"/> is true.</summary>
    public string DataTableParameterType => MatchedDataTable ? ParameterTypes.LastOrDefault() : null;
    /// <summary>True when <see cref="StepArgument"/> is a doc string.</summary>
    public bool MatchedDocString => StepArgument is DocString;
    /// <summary>The parameter type bound to the doc string argument, when <see cref="MatchedDocString"/> is true.</summary>
    public string DocStringParameterType => MatchedDocString ? ParameterTypes.LastOrDefault() : null;
}
