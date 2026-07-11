namespace Reqnroll.IdeSupport.LSP.Core.Completions;

/// <summary>
/// The result of a completion query: an ordered list of candidates plus an
/// <see cref="IsIncomplete"/> flag that maps to the LSP <c>CompletionList.isIncomplete</c> field.
/// </summary>
public sealed class CompletionResult
{
    /// <summary>An empty, complete completion result with no candidates.</summary>
    public static readonly CompletionResult Empty = new(Array.Empty<CompletionEntry>());

    /// <summary>Initializes a new instance of the <see cref="CompletionResult"/> class.</summary>
    public CompletionResult(IReadOnlyList<CompletionEntry> entries, bool isIncomplete = false)
    {
        Entries      = entries;
        IsIncomplete = isIncomplete;
    }

    /// <summary>Gets the ordered list of completion candidates.</summary>
    public IReadOnlyList<CompletionEntry> Entries      { get; }
    /// <summary>Gets whether more results exist beyond <see cref="Entries"/>, mapping to LSP <c>CompletionList.isIncomplete</c>.</summary>
    public bool                           IsIncomplete { get; }
}
