namespace Reqnroll.IdeSupport.LSP.Core.Editor.Completions.Matching;

/// <summary>
/// Default <see cref="ICompletionMatcher"/> that returns every candidate in a stable order
/// without filtering or scoring.  The LSP client applies its own built-in word-contains
/// filter.  <see cref="IsIncomplete"/> is <see langword="false"/> so one server round-trip
/// suffices for the whole typing session.
/// </summary>
public sealed class ReturnAllCompletionMatcher : ICompletionMatcher
{
    public bool IsIncomplete => false;

    public IReadOnlyList<ScoredCandidate> Rank(string typed, IReadOnlyList<StepCandidate> candidates)
        => candidates
            .Select(c => new ScoredCandidate(c.Sample, 0.0))
            .ToList();
}
