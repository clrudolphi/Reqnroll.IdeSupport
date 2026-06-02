using AwesomeAssertions;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Reqnroll;
using Reqnroll.IdeSupport.LSP.Server.Specs.Support;

namespace Reqnroll.IdeSupport.LSP.Server.Specs.StepDefinitions;

[Binding]
public sealed class ProtocolSteps
{
    private readonly LspScenarioContext _ctx;

    public ProtocolSteps(LspScenarioContext ctx) => _ctx = ctx;

    // ── Given ──────────────────────────────────────────────────────────────────

    [Given("the LSP server is started")]
    public async Task GivenTheLspServerIsStarted() => await _ctx.EnsureStartedAsync();

    [Given(@"the LSP server is started for IDE ""(.*)""")]
    public async Task GivenTheLspServerIsStartedForIde(string ide) => await _ctx.EnsureStartedAsync(ide);

    // ── When ───────────────────────────────────────────────────────────────────

    [When(@"the feature file ""(.*)"" is opened with")]
    public async Task WhenTheFeatureFileIsOpenedWith(string fileName, string content)
    {
        await _ctx.EnsureStartedAsync();
        var uri = _ctx.UriFor(fileName);
        _ctx.LastUri = uri;
        _ctx.LastDocumentText = content;
        _ctx.LastVersion = 1;
        _ctx.Harness.Client.OpenDocument(uri, 1, content);
        _ctx.LastTokens = await _ctx.Harness.Client.RequestSemanticTokensWhenReadyAsync(uri);
    }

    [When(@"the feature file ""(.*)"" is changed to")]
    public async Task WhenTheFeatureFileIsChangedTo(string fileName, string content)
    {
        var uri = _ctx.UriFor(fileName);
        _ctx.LastUri = uri;
        _ctx.LastDocumentText = content;
        _ctx.LastVersion += 1;
        _ctx.Harness.Client.ChangeDocument(uri, _ctx.LastVersion, content);
        _ctx.LastTokens = await _ctx.Harness.Client.RequestSemanticTokensWhenReadyAsync(uri);
    }

    [When(@"the feature file ""(.*)"" is closed")]
    public void WhenTheFeatureFileIsClosed(string fileName)
        => _ctx.Harness.Client.CloseDocument(_ctx.UriFor(fileName));

    [When("the semantic tokens are requested again")]
    public async Task WhenTheSemanticTokensAreRequestedAgain()
        => _ctx.LastTokens = await _ctx.Harness.Client.RequestSemanticTokensWhenReadyAsync(_ctx.LastUri!);

    [When("the semantic tokens are requested once")]
    public async Task WhenTheSemanticTokensAreRequestedOnce()
        => _ctx.LastTokens = await _ctx.Harness.Client.RequestSemanticTokensAsync(_ctx.LastUri!);

    [When(@"the project is announced with output assembly ""(.*)"" for ""(.*)""")]
    public void WhenTheProjectIsAnnounced(string outputAssembly, string fileName)
    {
        var projectFolder = _ctx.WorkspaceFolder;
        _ctx.Harness.Client.SendProjectLoaded(new
        {
            workspaceFolder = _ctx.WorkspaceFolder,
            projectFile = Path.Combine(projectFolder, "Sample.csproj"),
            projectFolder,
            outputAssemblyPath = Path.IsPathRooted(outputAssembly)
                ? outputAssembly
                : Path.Combine(projectFolder, outputAssembly),
            targetFrameworkMoniker = ".NETCoreApp,Version=v8.0",
            packageReferences = Array.Empty<object>()
        });
    }

    [When(@"the project is unloaded")]
    public void WhenTheProjectIsUnloaded()
        => _ctx.Harness.Client.SendProjectUnloaded(new
        {
            projectFile = Path.Combine(_ctx.WorkspaceFolder, "Sample.csproj")
        });

    // ── Then: handshake ─────────────────────────────────────────────────────────

    [Then("the server advertises a semantic tokens provider")]
    public void ThenTheServerAdvertisesASemanticTokensProvider()
        => GetLegend().Should().NotBeNull();

    [Then("the semantic tokens legend includes the token types")]
    public void ThenTheLegendIncludesTokenTypes(Table table)
    {
        var legend = GetLegend();
        var advertised = legend.TokenTypes.Select(t => t.ToString()).ToList();
        foreach (var row in table.Rows)
            advertised.Should().Contain(row["tokenType"]);
    }

    // ── Then: tokens ────────────────────────────────────────────────────────────

    [Then(@"the semantic tokens include a ""(.*)"" token for ""(.*)""")]
    public void ThenTheSemanticTokensIncludeATokenFor(string tokenType, string text)
    {
        var tokens = DecodeLast();
        tokens.Should().Contain(
            t => string.Equals(t.TokenType, tokenType, StringComparison.OrdinalIgnoreCase)
                 && t.Text.Trim() == text,
            $"a '{tokenType}' token covering '{text}' should be present. Got: " +
            string.Join(", ", tokens.Select(t => $"{t.TokenType}:'{t.Text}'")));
    }

    [Then(@"the semantic tokens do not include any ""(.*)"" token")]
    public void ThenTheSemanticTokensDoNotIncludeAnyTokenOfType(string tokenType)
    {
        var tokens = DecodeLast();
        tokens.Should().NotContain(
            t => string.Equals(t.TokenType, tokenType, StringComparison.OrdinalIgnoreCase));
    }

    [Then(@"the semantic tokens include a ""(.*)"" token with the ""(.*)"" modifier for ""(.*)""")]
    public void ThenTokenWithModifierFor(string tokenType, string modifier, string text)
    {
        var tokens = DecodeLast();
        tokens.Should().Contain(
            t => string.Equals(t.TokenType, tokenType, StringComparison.OrdinalIgnoreCase)
                 && t.Text.Trim() == text
                 && t.Modifiers.Any(m => string.Equals(m, modifier, StringComparison.OrdinalIgnoreCase)),
            $"a '{tokenType}'+'{modifier}' token covering '{text}' should be present");
    }

    [Then("the semantic tokens are non-overlapping")]
    public void ThenTheSemanticTokensAreNonOverlapping()
    {
        var tokens = DecodeLast().OrderBy(t => t.Line).ThenBy(t => t.StartChar).ToList();
        for (int i = 1; i < tokens.Count; i++)
        {
            var prev = tokens[i - 1];
            var cur = tokens[i];
            if (cur.Line != prev.Line) continue;
            (prev.StartChar + prev.Length).Should().BeLessThanOrEqualTo(
                cur.StartChar,
                $"token '{prev.Text}' ({prev.StartChar}+{prev.Length}) must not overlap '{cur.Text}' ({cur.StartChar})");
        }
    }

    [Then("no semantic tokens are returned")]
    public void ThenNoSemanticTokensAreReturned()
        => (_ctx.LastTokens is null || _ctx.LastTokens.Data.Length == 0).Should().BeTrue(
            "the document has no tags (e.g. after close), so no tokens should be produced");

    [Then("the server requests a semantic tokens refresh")]
    public async Task ThenTheServerRequestsASemanticTokensRefresh()
        => (await _ctx.Harness.WaitForRefreshAsync(minCount: 1)).Should().BeTrue(
            "the server should ask the client to refresh semantic tokens after a re-parse");

    // ── Helpers ─────────────────────────────────────────────────────────────────

    private SemanticTokensLegend GetLegend()
    {
        var provider = _ctx.Harness.ServerInitializeResult.Capabilities.SemanticTokensProvider;
        provider.Should().NotBeNull("the server should advertise a semantic tokens provider");
        return provider!.Legend;
    }

    private IReadOnlyList<DecodedToken> DecodeLast()
    {
        _ctx.LastTokens.Should().NotBeNull("semantic tokens should have been returned");
        return SemanticTokenDecoder.Decode(_ctx.LastTokens!, GetLegend(), _ctx.LastDocumentText);
    }
}
