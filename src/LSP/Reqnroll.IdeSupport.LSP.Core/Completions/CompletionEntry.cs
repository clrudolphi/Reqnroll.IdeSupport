namespace Reqnroll.IdeSupport.LSP.Core.Completions;

/// <summary>LSP-agnostic completion item kind, mapping to CompletionItemKind values.</summary>
public enum CompletionEntryKind
{
    /// <summary>A plain text completion candidate.</summary>
    Text    = 1,
    /// <summary>A language keyword completion candidate.</summary>
    Keyword = 14
}

/// <summary>
/// A single framework-neutral completion candidate returned by <see cref="ICompletionService"/>.
/// The Server handler maps this to an OmniSharp <c>CompletionItem</c>.
/// </summary>
public sealed record CompletionEntry(
    string              Label,
    string?             Detail,
    CompletionEntryKind Kind,
    string?             InsertText  = null,
    string?             FilterText  = null,
    string?             SortText    = null);
