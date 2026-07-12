using Reqnroll.IdeSupport.LSP.Server.Features.Rename;

namespace Reqnroll.IdeSupport.LSP.Server.Tests.Features.Rename;

public class CSharpAttributeLiteralResolverTests
{
    // ── ReconcileParameterTokens unit behaviour ─────────────────────────────────────────

    [Theory]
    // regex-form edit over a Cucumber source → Cucumber type retained
    [InlineData("the first number is {int}", "the first no is (.*)", "the first no is {int}")]
    // Cucumber-form edit over a Cucumber source → verbatim
    [InlineData("the first number is {int}", "the first no is {int}", "the first no is {int}")]
    // regex source stays regex
    [InlineData("the first number is (.*)", "the first no is (.*)", "the first no is (.*)")]
    // multiple params, mixed forms → each slot takes the source token positionally
    [InlineData("a {int} b {string}", "x (.*) y (.*)", "x {int} y {string}")]
    // no parameters → verbatim
    [InlineData("just text", "renamed text", "renamed text")]
    // slot-count mismatch → user text honoured verbatim
    [InlineData("a {int}", "a {int} {word}", "a {int} {word}")]
    public void ReconcileParameterTokens_preserves_original_slot_tokens(
        string source, string newName, string expected)
    {
        CSharpAttributeLiteralResolver.ReconcileParameterTokens(source, newName).Should().Be(expected);
    }
}
