namespace Reqnroll.IdeSupport.LSP.Core.Completions;

/// <summary>LSP-agnostic completion item kind, mapping to CompletionItemKind values.</summary>
public enum CompletionEntryKind
{
    /// <summary>Gets or sets the text.</summary>
    Text    = 1,
    /// <summary>Gets or sets the keyword.</summary>
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
