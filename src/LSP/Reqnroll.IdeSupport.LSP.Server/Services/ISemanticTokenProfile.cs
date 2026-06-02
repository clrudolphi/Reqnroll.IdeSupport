using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Reqnroll.IdeSupport.LSP.Core.Editor.Services.Parsing.GherkinDocuments;

namespace Reqnroll.IdeSupport.LSP.Server.Services;

/// <summary>
/// Defines the semantic token legend and the DeveroomTag→token mapping for a specific IDE.
/// </summary>
/// <remarks>
/// <para>
/// Different IDEs expose different built-in token type names and assign different default
/// display attributes (colours, styles) to those names.  Visual Studio, VS Code, and
/// JetBrains Rider each have their own classification/token infrastructure layered on top
/// of the LSP semantic token protocol.  A profile encapsulates both:
/// </para>
/// <list type="bullet">
///   <item>
///     <term>Legend</term>
///     <description>
///       The <see cref="SemanticTokensLegend"/> advertised to the IDE in the
///       <c>initialize</c> response.  Different IDEs may require different token
///       type names to trigger the desired built-in colours.
///     </description>
///   </item>
///   <item>
///     <term>Mapping</term>
///     <description>
///       The <see cref="TryGetToken"/> mapping from <see cref="DeveroomTag"/> types
///       to (tokenTypeIndex, tokenModifierBitset) pairs relative to the profile's own legend.
///     </description>
///   </item>
/// </list>
/// <para>
/// The active profile is selected at server startup via the <c>--ide</c> command-line
/// argument and registered as a singleton <see cref="ISemanticTokenProfile"/> in DI.
/// </para>
/// </remarks>
public interface ISemanticTokenProfile
{
    /// <summary>
    /// A short identifier for this profile, used in log messages.
    /// Matches the <c>--ide</c> argument value that selects it (e.g. <c>"visualstudio"</c>).
    /// </summary>
    string ProfileId { get; }

    /// <summary>
    /// The token-type legend advertised to the IDE client in the <c>initialize</c> response.
    /// Indices into <see cref="SemanticTokensLegend.TokenTypes"/> and
    /// <see cref="SemanticTokensLegend.TokenModifiers"/> are used in the encoded token data.
    /// </summary>
    SemanticTokensLegend Legend { get; }

    /// <summary>
    /// Maps a <see cref="DeveroomTag"/> to a zero-based index into <see cref="Legend"/>
    /// token types and a modifier bitmask.
    /// </summary>
    /// <param name="tag">The tag to map.</param>
    /// <param name="tokenTypeIndex">
    /// Zero-based index into <see cref="SemanticTokensLegend.TokenTypes"/>.
    /// Undefined when the method returns <see langword="false"/>.
    /// </param>
    /// <param name="tokenModifierBitset">
    /// Bitmask over <see cref="SemanticTokensLegend.TokenModifiers"/> (bit 0 = modifier[0], etc.).
    /// Pass 0 for no modifiers.  Undefined when the method returns <see langword="false"/>.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the tag should produce a semantic token;
    /// <see langword="false"/> if it should be silently skipped.
    /// </returns>
    bool TryGetToken(DeveroomTag tag, out int tokenTypeIndex, out int tokenModifierBitset);
}
