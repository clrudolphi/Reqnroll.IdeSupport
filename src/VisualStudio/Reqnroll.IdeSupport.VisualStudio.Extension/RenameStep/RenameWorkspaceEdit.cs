#nullable enable

using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Reqnroll.IdeSupport.VisualStudio.Extension.RenameStep;

/// <summary>
/// Client-side result for a <c>textDocument/rename</c> response (Step Rename refactoring).
/// Contains pre-parsed, sorted text edits grouped by local file path.
/// </summary>
internal sealed class RenameWorkspaceEdit
{
    /// <summary>
    /// Map from local file path to its sorted list of text edits (bottom-to-top).
    /// </summary>
    public Dictionary<string, List<TextEditItem>> FileEdits { get; } = new();
}

/// <summary>A single position-indexed text replacement within a document.</summary>
internal sealed record TextEditItem(
    /// <summary>0-based start line of the replaced range.</summary>
    int    StartLine,
    /// <summary>0-based start character of the replaced range.</summary>
    int    StartChar,
    /// <summary>0-based end line of the replaced range.</summary>
    int    EndLine,
    /// <summary>0-based end character of the replaced range.</summary>
    int    EndChar,
    /// <summary>Replacement text.</summary>
    string NewText);
