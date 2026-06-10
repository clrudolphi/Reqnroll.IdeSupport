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
    /// Converts a <see cref="SourceLocation"/> to an LSP <see cref="Location"/> for use in
    /// <c>textDocument/definition</c> responses.  Always produces a zero-width range at the
    /// start position — consistent with LSP convention where the definition range identifies
    /// the symbol, not its body.  End-position data from the discovery layer (e.g. PDB
    /// sequence-point spans from the Connector) is intentionally discarded here.
    /// </summary>
    public static Location ToLspLocation(this SourceLocation loc)
    {
        // SourceLocation is 1-based; LSP positions are 0-based.
        var line = loc.SourceFileLine - 1;
        var ch   = loc.SourceFileColumn - 1;

        return new Location
        {
            Uri   = DocumentUri.FromFileSystemPath(loc.SourceFile),
            Range = new LspRange(
                new Position(line, ch),
                new Position(line, ch))
        };
    }
}
