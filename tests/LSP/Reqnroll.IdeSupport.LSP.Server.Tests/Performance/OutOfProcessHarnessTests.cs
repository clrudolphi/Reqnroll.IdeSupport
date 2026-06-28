#nullable enable

using System.Threading.Tasks;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Reqnroll.IdeSupport.LSP.Server.Benchmarks.Corpus;
using Reqnroll.IdeSupport.LSP.Server.Benchmarks.Harness;
using Reqnroll.IdeSupport.LSP.Server.Benchmarks.Scenarios;

namespace Reqnroll.IdeSupport.LSP.Server.Tests.Performance;

/// <summary>
/// Self-test for the out-of-process transport: spawns the built server <b>exe</b> and talks to it
/// over stdio (the production transport), confirming the harness drives a real separate process.
/// The server exe is produced by the project reference, so it is present after building this suite.
/// </summary>
public class OutOfProcessHarnessTests
{
    [Fact]
    public async Task Spawned_server_over_stdio_answers_requests()
    {
        var exe = ServerExeLocator.Find();
        var corpusRoot = CorpusLocator.FindCorpusRoot();

        await using var harness = new BenchmarkLspHarness();
        await harness.StartOutOfProcessAsync(corpusRoot, exe);

        var features = await InteractiveScenarios.OpenFeaturesAsync(harness, corpusRoot, count: 1);
        var tokens = await harness.RequestSemanticTokensAsync(features[0].Uri);

        tokens.Should().NotBeNull($"the spawned server should answer; stderr was:\n{harness.ServerStandardError}");
        tokens!.Data.Should().NotBeEmpty();
    }
}
