using AwesomeAssertions;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Reqnroll;
using Reqnroll.IdeSupport.LSP.Server.Specs.Support;

namespace Reqnroll.IdeSupport.LSP.Server.Specs.StepDefinitions;

[Binding]
public sealed class CodeFoldingSteps
{
    private readonly LspScenarioContext _ctx;

    public CodeFoldingSteps(LspScenarioContext ctx) => _ctx = ctx;

    // -- When ------------------------------------------------------------------

    [When("the code folding ranges are requested for \"(.*)\"")]
    public async Task WhenTheCodeFoldingRangesAreRequestedFor(string fileName)
    {
        await _ctx.EnsureStartedAsync().ConfigureAwait(false);
        var uri = _ctx.UriFor(fileName);
        _ctx.LastFoldingRanges = await _ctx.Harness.Client
            .RequestFoldingRangeAsync(uri)
            .ConfigureAwait(false);
    }

    // -- Then ------------------------------------------------------------------

    [Then("the folding ranges are empty")]
    public void ThenTheFoldingRangesAreEmpty()
    {
        var count = _ctx.LastFoldingRanges?.Count() ?? 0;
        count.Should().Be(0, "a file with no foldable regions should yield no folding ranges");
    }

    [Then("the folding range count is (\\d+)")]
    public void ThenTheFoldingRangeCountIs(int expected)
    {
        AllFoldingRanges().Should().HaveCount(expected);
    }

    [Then("a folding range exists from line (\\d+) to line (\\d+)")]
    public void ThenAFoldingRangeExistsFromLineToLine(int startLine, int endLine)
    {
        var ranges = AllFoldingRanges();
        ranges.Should().Contain(
            r => r.StartLine == startLine && r.EndLine == endLine,
            $"a folding range [{startLine}..{endLine}] should exist. Got: {FormatRanges(ranges)}");
    }

    // -- Helpers ---------------------------------------------------------------

    private IEnumerable<FoldingRange> AllFoldingRanges()
        => _ctx.LastFoldingRanges ?? Enumerable.Empty<FoldingRange>();

    private static string FormatRanges(IEnumerable<FoldingRange> ranges)
        => string.Join(", ", ranges.Select(r => $"[{r.StartLine}..{r.EndLine}]"));
}
