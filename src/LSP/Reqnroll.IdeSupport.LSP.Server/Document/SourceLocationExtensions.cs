#nullable enable

using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Reqnroll.IdeSupport.LSP.Core.Discovery;
using LspRange = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;

namespace Reqnroll.IdeSupport.LSP.Server.Document;

/// <summary>
/// Converts <see cref="SourceLocation"/> values (1-based, discovery layer) to OmniSharp LSP types.
/// </summary>
public static class SourceLocationExtensions
{
    /// <summary>
    /// Converts a <see cref="SourceLocation"/> to an LSP <see cref="Location"/>.
    /// Uses the end-position fields when present; otherwise produces a degenerate (zero-length)
    /// range at the start position so clients can navigate to the right line without a method span.
    /// </summary>
    public static Location ToLspLocation(this SourceLocation loc)
    {
        // SourceLocation is 1-based; LSP positions are 0-based.
        var startLine = loc.SourceFileLine - 1;
        var startChar = loc.SourceFileColumn - 1;
        var endLine   = loc.HasEndPosition ? loc.SourceFileEndLine!.Value  - 1 : startLine;
        var endChar   = loc.HasEndPosition ? loc.SourceFileEndColumn!.Value - 1 : startChar;

        return new Location
        {
            Uri   = DocumentUri.FromFileSystemPath(loc.SourceFile),
            Range = new LspRange(
                new Position(startLine, startChar),
                new Position(endLine,   endChar))
        };
    }
}
