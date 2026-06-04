using System.Collections.Generic;
using AwesomeAssertions;
using Reqnroll.IdeSupport.VisualStudio.Extension.Classification;
using Xunit;

namespace Reqnroll.VisualStudio.Tests.Classification;

public class SemanticTokenClassificationStoreTests
{
    // ── NormalizeKey ────────────────────────────────────────────────────────────

    [Fact]
    public void NormalizeKey_treats_a_file_uri_and_the_equivalent_local_path_as_the_same_key()
    {
        // The interceptor sees a file:// URI; the classifier sees ITextDocument.FilePath.
        // Both must normalize to the same key (incl. drive-letter case) or nothing colours.
        var fromUri = SemanticTokenClassificationStore.NormalizeKey("file:///C:/Users/x/Features/A.feature");
        var fromPath = SemanticTokenClassificationStore.NormalizeKey(@"c:\Users\x\Features\A.feature");

        fromUri.Should().NotBeNull();
        fromUri.Should().Be(fromPath);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void NormalizeKey_returns_null_for_null_or_empty(string? input)
        => SemanticTokenClassificationStore.NormalizeKey(input).Should().BeNull();

    // ── Set / TryGet / event ────────────────────────────────────────────────────

    [Fact]
    public void SetTokens_round_trips_through_TryGetTokens_and_raises_TokensChanged()
    {
        var store = new SemanticTokenClassificationStore();
        var key = SemanticTokenClassificationStore.NormalizeKey(@"c:\w\A.feature")!;
        string? changedKey = null;
        store.TokensChanged += k => changedKey = k;

        store.SetTokens(key, new List<ClassifiedToken> { new(0, 0, 5, "reqnroll.keyword") });

        changedKey.Should().Be(key, "the classifier subscribes to TokensChanged to trigger a recolor");
        store.TryGetTokens(key, out var got).Should().BeTrue();
        got.Should().ContainSingle();
        got[0].TokenType.Should().Be("reqnroll.keyword");
        got[0].Length.Should().Be(5);
    }

    [Fact]
    public void TryGetTokens_returns_false_for_an_unknown_key()
    {
        var store = new SemanticTokenClassificationStore();

        store.TryGetTokens("nope", out var got).Should().BeFalse();
        got.Should().BeEmpty();
    }
}
