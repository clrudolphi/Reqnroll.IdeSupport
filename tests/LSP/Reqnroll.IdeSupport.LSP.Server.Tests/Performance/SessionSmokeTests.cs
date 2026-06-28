#nullable enable

using System.Linq;
using System.Threading.Tasks;
using Reqnroll.IdeSupport.LSP.Server.Benchmarks.Corpus;
using Reqnroll.IdeSupport.LSP.Server.Benchmarks.Harness;
using Reqnroll.IdeSupport.LSP.Server.Benchmarks.Latency;
using Reqnroll.IdeSupport.LSP.Server.Benchmarks.Scenarios;

namespace Reqnroll.IdeSupport.LSP.Server.Tests.Performance;

/// <summary>
/// Self-test for the editing-session scenario: drives the real in-process server through a few
/// edit bursts and asserts it produces populated per-operation results plus activity stats.
/// </summary>
public class SessionSmokeTests
{
    [Fact]
    public async Task Session_drives_the_real_server_under_load_and_produces_results_and_stats()
    {
        var corpusRoot = CorpusLocator.FindCorpusRoot();

        await using var harness = new BenchmarkLspHarness();
        await harness.StartAsync(corpusRoot);

        var features = await InteractiveScenarios.OpenFeaturesAsync(harness, corpusRoot, count: 3);
        var options = new SessionOptions(
            Warmup: 1, Bursts: 6, SupersedeRate: 0.5, ThinkMs: 0, TypingGapMs: 0, NavigateEveryNthBurst: 3);

        var result = await new SessionScenario(harness, features, options).RunAsync();

        // The burst pulls (semantic tokens, completion, outline, folding) all produced samples.
        var ops = result.Results.Select(r => r.Target.Operation).ToArray();
        ops.Should().Contain(PerfTargets.SemanticTokensFull.Operation);
        ops.Should().Contain(PerfTargets.CompletionStep.Operation);
        ops.Should().Contain(PerfTargets.DocumentSymbol.Operation);
        ops.Should().Contain(PerfTargets.FoldingRange.Operation);

        result.Results.Should().OnlyContain(r => r.Summary.SampleCount > 0);
        result.Stats.Bursts.Should().Be(6);
        result.Stats.RequestsIssued.Should().BeGreaterThan(0);
        result.Stats.CancellationRatePct.Should().BeInRange(0, 100);
    }
}
