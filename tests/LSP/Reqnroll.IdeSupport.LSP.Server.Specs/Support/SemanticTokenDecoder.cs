using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Reqnroll.IdeSupport.LSP.Server.Specs.Support;

/// <summary>One decoded semantic token with its resolved type name and covered text.</summary>
public sealed record DecodedToken(
    int Line,
    int StartChar,
    int Length,
    string TokenType,
    IReadOnlyList<string> Modifiers,
    string Text);

/// <summary>
/// Decodes the LSP 5-int delta encoding (deltaLine, deltaStartChar, length, tokenTypeIndex,
/// tokenModifierBitset) back into absolute, named <see cref="DecodedToken"/> values using the
/// legend the server advertised, resolving the covered text against the original document.
/// </summary>
public static class SemanticTokenDecoder
{
    public static IReadOnlyList<DecodedToken> Decode(
        SemanticTokens tokens, SemanticTokensLegend legend, string documentText)
    {
        var types = legend.TokenTypes.ToArray();
        var modifiers = legend.TokenModifiers.ToArray();
        var lines = SplitLines(documentText);

        var data = tokens.Data.ToArray();
        var result = new List<DecodedToken>(data.Length / 5);

        int line = 0, ch = 0;
        for (int i = 0; i + 4 < data.Length; i += 5)
        {
            int deltaLine = data[i];
            int deltaChar = data[i + 1];
            int length = data[i + 2];
            int typeIdx = data[i + 3];
            int modBits = data[i + 4];

            if (deltaLine == 0) ch += deltaChar;
            else { line += deltaLine; ch = deltaChar; }

            var typeName = typeIdx >= 0 && typeIdx < types.Length ? types[typeIdx].ToString() : $"#{typeIdx}";
            var mods = new List<string>();
            for (int b = 0; b < modifiers.Length; b++)
                if ((modBits & (1 << b)) != 0)
                    mods.Add(modifiers[b].ToString());

            var text = SliceText(lines, line, ch, length);
            result.Add(new DecodedToken(line, ch, length, typeName, mods, text));
        }

        return result;
    }

    private static string[] SplitLines(string text) =>
        text.Replace("\r\n", "\n").Split('\n');

    private static string SliceText(string[] lines, int line, int start, int length)
    {
        if (line < 0 || line >= lines.Length) return string.Empty;
        var s = lines[line];
        if (start < 0 || start > s.Length) return string.Empty;
        return s.Substring(start, Math.Min(length, s.Length - start));
    }
}
