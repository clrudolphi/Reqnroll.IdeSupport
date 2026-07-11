
namespace Reqnroll.IdeSupport.LSP.Core.Matching;

/// <summary>The span of a single captured parameter within a step's text (character index and length).</summary>
public struct MatchedStepTextParameter
{
    /// <summary>The zero-based character index where the parameter starts within the step text.</summary>
    public int Index;
    /// <summary>The length, in characters, of the parameter's text.</summary>
    public int Length;

    /// <summary>Creates a parameter span.</summary>
    public MatchedStepTextParameter(int index, int length)
    {
        Index = index;
        Length = length;
    }
}
