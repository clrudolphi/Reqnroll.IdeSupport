namespace Reqnroll.IdeSupport.LSP.Core.Completions;

/// <summary>
/// The result of a completion query: an ordered list of candidates plus an
/// <see cref="IsIncomplete"/> flag that maps to the LSP <c>CompletionList.isIncomplete</c> field.
/// </summary>
public sealed class CompletionResult
{
    public static readonly CompletionResult Empty = new(Array.Empty<CompletionEntry>());

    public CompletionResult(IReadOnlyList<CompletionEntry> entries, bool isIncomplete = false)
    {
        Entries      = entries;
        IsIncomplete = isIncomplete;
    }

    public IReadOnlyList<CompletionEntry> Entries      { get; }
    public bool                           IsIncomplete { get; }
}
