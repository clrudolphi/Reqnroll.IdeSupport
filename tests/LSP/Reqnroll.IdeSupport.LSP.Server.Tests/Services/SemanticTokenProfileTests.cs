using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Reqnroll.IdeSupport.LSP.Core.Document;
using Reqnroll.IdeSupport.LSP.Core.Editor.Services.Parsing.GherkinDocuments;
using Reqnroll.IdeSupport.LSP.Server.Services;

namespace Reqnroll.IdeSupport.LSP.Server.Tests.Services;

public class SemanticTokenProfileTests
{
    // ── Factory ────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(SemanticTokenProfileFactory.VisualStudio)]
    [InlineData("VisualStudio")]      // case-insensitive
    [InlineData("  visualstudio  ")]  // trimmed
    [InlineData(SemanticTokenProfileFactory.VsCode)]
    [InlineData(SemanticTokenProfileFactory.Rider)]
    [InlineData("totally-unknown-ide")]
    [InlineData(null)]
    public void Create_always_returns_a_usable_profile(string? ideId)
    {
        var profile = SemanticTokenProfileFactory.Create(ideId);

        profile.Should().NotBeNull();
        profile.Legend.Should().NotBeNull();
        profile.ProfileId.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Create_returns_visual_studio_profile_for_visualstudio_identifier()
    {
        SemanticTokenProfileFactory.Create(SemanticTokenProfileFactory.VisualStudio)
            .Should().BeOfType<VisualStudioSemanticTokenProfile>();
    }

    [Fact]
    public void Create_falls_back_to_visual_studio_profile_for_unknown_identifier()
    {
        SemanticTokenProfileFactory.Create("nope")
            .Should().BeOfType<VisualStudioSemanticTokenProfile>();
    }

    // ── VisualStudioSemanticTokenProfile.Legend ────────────────────────────────

    [Fact]
    public void VisualStudioProfile_legend_declares_the_expected_token_types_and_modifiers()
    {
        var profile = new VisualStudioSemanticTokenProfile();

        profile.ProfileId.Should().Be(SemanticTokenProfileFactory.VisualStudio);
        profile.Legend.TokenTypes.Should().Contain(SemanticTokenType.Keyword);
        profile.Legend.TokenTypes.Should().Contain(SemanticTokenType.Parameter);
        profile.Legend.TokenTypes.Should().Contain(SemanticTokenType.Function);
        profile.Legend.TokenTypes.Should().Contain(SemanticTokenType.Regexp);
        profile.Legend.TokenModifiers.Should().Contain(SemanticTokenModifier.Declaration);
        profile.Legend.TokenModifiers.Should().Contain(SemanticTokenModifier.Deprecated);
    }

    // ── VisualStudioSemanticTokenProfile.TryGetToken ───────────────────────────

    private static readonly IGherkinTextSnapshot Snapshot = new StubSnapshot();

    private static DeveroomTag Tag(string type) =>
        new(type, new GherkinRange(Snapshot, 0, 1));

    [Theory]
    [InlineData(DeveroomTagTypes.StepKeyword)]
    [InlineData(DeveroomTagTypes.DefinitionLineKeyword)]
    [InlineData(DeveroomTagTypes.StepParameter)]
    [InlineData(DeveroomTagTypes.Tag)]
    [InlineData(DeveroomTagTypes.Comment)]
    [InlineData(DeveroomTagTypes.DefinedStep)]
    [InlineData(DeveroomTagTypes.UndefinedStep)]
    [InlineData(DeveroomTagTypes.BindingError)]
    [InlineData(DeveroomTagTypes.DataTableHeader)]
    public void TryGetToken_maps_known_leaf_tags(string tagType)
    {
        var profile = new VisualStudioSemanticTokenProfile();

        profile.TryGetToken(Tag(tagType), out var typeIndex, out _).Should().BeTrue();
        typeIndex.Should().BeInRange(0, profile.Legend.TokenTypes.Count() - 1);
    }

    [Fact]
    public void TryGetToken_marks_undefined_step_as_deprecated_regexp()
    {
        var profile = new VisualStudioSemanticTokenProfile();
        var regexpIndex = profile.Legend.TokenTypes.ToList().IndexOf(SemanticTokenType.Regexp);
        var deprecatedBit = 1 << profile.Legend.TokenModifiers.ToList()
            .IndexOf(SemanticTokenModifier.Deprecated);

        profile.TryGetToken(Tag(DeveroomTagTypes.UndefinedStep), out var typeIndex, out var modBits)
            .Should().BeTrue();

        typeIndex.Should().Be(regexpIndex);
        (modBits & deprecatedBit).Should().Be(deprecatedBit);
    }

    [Fact]
    public void TryGetToken_marks_definition_line_keyword_as_declaration()
    {
        var profile = new VisualStudioSemanticTokenProfile();
        var declarationBit = 1 << profile.Legend.TokenModifiers.ToList()
            .IndexOf(SemanticTokenModifier.Declaration);

        profile.TryGetToken(Tag(DeveroomTagTypes.DefinitionLineKeyword), out _, out var modBits)
            .Should().BeTrue();

        (modBits & declarationBit).Should().Be(declarationBit);
    }

    [Fact]
    public void TryGetToken_returns_false_for_container_or_unknown_tags()
    {
        var profile = new VisualStudioSemanticTokenProfile();

        profile.TryGetToken(Tag("SomeUnmappedContainerTag"), out var typeIndex, out var modBits)
            .Should().BeFalse();
        typeIndex.Should().Be(0);
        modBits.Should().Be(0);
    }

    // ── Minimal snapshot stub (TryGetToken never reads the range) ───────────────

    private sealed class StubSnapshot : IGherkinTextSnapshot
    {
        public int Version => 1;
        public int Length => 1;
        public int LineCount => 1;
        public string GetText() => " ";
        public IGherkinTextSnapshotLine GetLineFromLineNumber(int lineNumber) => new StubLine();

        private sealed class StubLine : IGherkinTextSnapshotLine
        {
            public int LineNumber => 0;
            public int Start => 0;
            public int End => 1;
            public string GetText() => " ";
        }
    }
}
