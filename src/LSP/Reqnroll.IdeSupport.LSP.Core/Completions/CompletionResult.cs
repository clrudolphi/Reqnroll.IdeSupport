namespace Reqnroll.IdeSupport.LSP.Core.Completions;

/// <summary>
/// The result of a completion query: an ordered list of candidates plus an
/// <see cref="IsIncomplete"/> flag that maps to the LSP <c>CompletionList.isIncomplete</c> field.
/// </summary>
public sealed class CompletionResult
{
    /// <summary>Gets or sets the empty.</summary>
    public static readonly CompletionResult Empty = new(Array.Empty<CompletionEntry>());

    /// <summary>Initializes a new instance of the <see cref="CompletionResult"/> class.</summary>
    public CompletionResult(IReadOnlyList<CompletionEntry> entries, bool isIncomplete = false)
    {
        Entries      = entries;
        IsIncomplete = isIncomplete;
    }

    /// <summary>Gets or sets the entries.</summary>
    public IReadOnlyList<CompletionEntry> Entries      { get; }
    /// <summary>Gets or sets the is incomplete.</summary>
    public bool                           IsIncomplete { get; }
}
