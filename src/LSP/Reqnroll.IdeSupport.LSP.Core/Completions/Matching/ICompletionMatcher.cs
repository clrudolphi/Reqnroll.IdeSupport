namespace Reqnroll.IdeSupport.LSP.Core.Completions.Matching;

/// <summary>A step-completion candidate before ranking.</summary>
public readonly record struct StepCandidate(string Sample, int UsageCount);

/// <summary>A ranked step-completion candidate.</summary>
public readonly record struct ScoredCandidate(string Sample, double Score);

/// <summary>
/// Ranks a list of <see cref="StepCandidate"/> objects against the text the user has typed
/// after the step keyword. The default implementation (<see cref="ReturnAllCompletionMatcher"/>)
/// returns all candidates unfiltered and lets the LSP client do the filtering.
/// The FuzzySharp contingency implementation performs server-side two-tier ranking.
/// </summary>
public interface ICompletionMatcher
{
    /// <summary>
    /// Returns candidates ranked best-first. Implementations may trim the list.
    /// </summary>
    IReadOnlyList<ScoredCandidate> Rank(string typed, IReadOnlyList<StepCandidate> candidates);

    /// <summary>
    /// When <see langword="true"/> the server marks the returned <c>CompletionList</c>
    /// as incomplete, causing the LSP client to re-query on every keystroke.
    /// </summary>
    bool IsIncomplete { get; }
}
