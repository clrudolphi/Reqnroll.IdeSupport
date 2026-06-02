using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Reqnroll.IdeSupport.LSP.Core.Editor.Services.Parsing.GherkinDocuments;

namespace Reqnroll.IdeSupport.LSP.Server.Services;

/// <summary>
/// Semantic token profile for Visual Studio.
/// </summary>
/// <remarks>
/// <para>
/// Uses the standard LSP token type names that Visual Studio maps to its built-in
/// classification types.  The mapping was designed for VS 2022+ with the new
/// VisualStudio.Extensibility SDK.
/// </para>
/// <para>
/// Notable VS-specific choices:
/// <list type="bullet">
///   <item>
///     <term>parameter</term>
///     <description>
///       Maps to VS's "Parameter Name" classification — light blue in the dark theme,
///       but no distinct colour in the light theme by default.  A future iteration may
///       register a custom VS classification with a Reqnroll-specific default colour.
///     </description>
///   </item>
///   <item>
///     <term>regexp + deprecated</term>
///     <description>
///       Used for <c>UndefinedStep</c> and <c>BindingError</c>; VS renders the
///       <c>deprecated</c> modifier as a strikethrough.
///     </description>
///   </item>
/// </list>
/// </para>
/// </remarks>
public sealed class VisualStudioSemanticTokenProfile : ISemanticTokenProfile
{
    // ── Token type indices ────────────────────────────────────────────────────
    private enum T
    {
        Keyword   = 0,   // DefinitionLineKeyword, StepKeyword
        String    = 1,   // Description, DocString
        Parameter = 2,   // StepParameter, ScenarioOutlinePlaceholder
        Variable  = 3,   // Tag (@tag)
        Comment   = 4,   // Comment
        Class     = 5,   // (reserved — unused in current mapping)
        Function  = 6,   // DefinedStep
        Regexp    = 7,   // UndefinedStep, BindingError
        Struct    = 8,   // DataTableHeader
        Event     = 9,   // (reserved — unused in current mapping)
    }

    // ── Token modifier bitmasks ───────────────────────────────────────────────
    private const int None        = 0;
    private const int Declaration = 1 << 0;  // modifier index 0
    private const int Deprecated  = 1 << 1;  // modifier index 1

    // ── ISemanticTokenProfile ─────────────────────────────────────────────────

    public string ProfileId => SemanticTokenProfileFactory.VisualStudio;

    public SemanticTokensLegend Legend { get; } = new SemanticTokensLegend
    {
        TokenTypes = new Container<SemanticTokenType>(
        [
            SemanticTokenType.Keyword,    // 0
            SemanticTokenType.String,     // 1
            SemanticTokenType.Parameter,  // 2
            SemanticTokenType.Variable,   // 3
            SemanticTokenType.Comment,    // 4
            SemanticTokenType.Class,      // 5
            SemanticTokenType.Function,   // 6
            SemanticTokenType.Regexp,     // 7
            SemanticTokenType.Struct,     // 8
            SemanticTokenType.Event,      // 9
        ]),
        TokenModifiers = new Container<SemanticTokenModifier>(
        [
            SemanticTokenModifier.Declaration,  // bit 0
            SemanticTokenModifier.Deprecated,   // bit 1
        ]),
    };

    /// <inheritdoc/>
    public bool TryGetToken(DeveroomTag tag, out int tokenTypeIndex, out int tokenModifierBitset)
    {
        tokenModifierBitset = None;

        switch (tag.Type)
        {
            case DeveroomTagTypes.DefinitionLineKeyword:
                tokenTypeIndex    = (int)T.Keyword;
                tokenModifierBitset = Declaration;
                return true;

            case DeveroomTagTypes.StepKeyword:
                tokenTypeIndex = (int)T.Keyword;
                return true;

            case DeveroomTagTypes.Description:
            case DeveroomTagTypes.DocString:
                tokenTypeIndex = (int)T.String;
                return true;

            case DeveroomTagTypes.StepParameter:
            case DeveroomTagTypes.ScenarioOutlinePlaceholder:
                tokenTypeIndex = (int)T.Parameter;
                return true;

            case DeveroomTagTypes.Tag:
                tokenTypeIndex = (int)T.Variable;
                return true;

            case DeveroomTagTypes.Comment:
                tokenTypeIndex = (int)T.Comment;
                return true;

            case DeveroomTagTypes.DefinedStep:
                tokenTypeIndex = (int)T.Function;
                return true;

            case DeveroomTagTypes.UndefinedStep:
            case DeveroomTagTypes.BindingError:
                tokenTypeIndex      = (int)T.Regexp;
                tokenModifierBitset = Deprecated;
                return true;

            case DeveroomTagTypes.DataTableHeader:
                tokenTypeIndex      = (int)T.Struct;
                tokenModifierBitset = Declaration;
                return true;

            // Block/container tags and unhandled artifacts — skip.
            default:
                tokenTypeIndex = 0;
                return false;
        }
    }
}
