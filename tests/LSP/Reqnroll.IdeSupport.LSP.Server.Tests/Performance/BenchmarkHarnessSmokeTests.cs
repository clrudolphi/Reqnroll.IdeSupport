#nullable enable

using System.Threading.Tasks;
using Reqnroll.IdeSupport.LSP.Server.Benchmarks.Corpus;
using Reqnroll.IdeSupport.LSP.Server.Benchmarks.Harness;
using Reqnroll.IdeSupport.LSP.Server.Benchmarks.Scenarios;

namespace Reqnroll.IdeSupport.LSP.Server.Tests.Performance;

/// <summary>
/// Harness self-test (implementation plan §2): drives the real in-process server against a tiny
/// corpus subset with low iteration counts and asserts the benchmark produces a populated result.
/// Guards against the harness silently measuring nothing (e.g. a handler returning null for the
/// corpus URIs, or positions that never hit a step).
/// </summary>
public class BenchmarkHarnessSmokeTests
{
    [Fact]
    public async Task Harness_drives_the_real_server_and_produces_samples()
    {
        var corpusRoot = CorpusLocator.FindCorpusRoot();

        await using var harness = new BenchmarkLspHarness();
        await harness.StartAsync(corpusRoot);

        var features = await InteractiveScenarios.OpenFeaturesAsync(harness, corpusRoot, count: 2);
        features.Should().NotBeEmpty();

        var scenarios = new InteractiveScenarios(harness, features, warmup: 1, measured: 3);

        var semanticTokens = await scenarios.SemanticTokensAsync();
        semanticTokens.SampleCount.Should().Be(3);
        semanticTokens.P95Ms.Should().BeGreaterThanOrEqualTo(0);

        var keyword = await scenarios.KeywordCompletionAsync();
        keyword.SampleCount.Should().Be(3);
    }
}
