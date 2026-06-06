using LspRange = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Reqnroll.IdeSupport.LSP.Core.Document;

namespace Reqnroll.IdeSupport.LSP.Server.Document;

/// <summary>
/// Converts <see cref="GherkinRange"/> positions to OmniSharp LSP types.
/// The pure geometry math (offset → line/character) lives on <see cref="GherkinRange"/> itself;
/// this class adds only the OmniSharp-typed surface that belongs in the server layer.
/// </summary>
public static class GherkinRangeExtensions
{
    public static Position ToLspStartPosition(this GherkinRange range)
    {
        var (line, character) = range.StartLinePosition;
        return new Position(line, character);
    }

    public static Position ToLspEndPosition(this GherkinRange range)
    {
        var (line, character) = range.EndLinePosition;
        return new Position(line, character);
    }

    public static LspRange ToLspRange(this GherkinRange range)
        => new(range.ToLspStartPosition(), range.ToLspEndPosition());
}
