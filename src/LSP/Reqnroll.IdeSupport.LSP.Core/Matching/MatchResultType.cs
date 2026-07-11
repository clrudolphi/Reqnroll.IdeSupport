namespace Reqnroll.IdeSupport.LSP.Core.Matching;

/// <summary>The classification of a step-to-step-definition match outcome.</summary>
public enum MatchResultType
{
    /// <summary>No match has been computed yet.</summary>
    Unknown,
    /// <summary>No step definition matched the step text.</summary>
    Undefined,
    /// <summary>Exactly one step definition matched the step text.</summary>
    Defined,
    /// <summary>More than one step definition matched the step text.</summary>
    Ambiguous
}
