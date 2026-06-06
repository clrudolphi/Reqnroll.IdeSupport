using AwesomeAssertions;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Reqnroll;
using Reqnroll.IdeSupport.LSP.Server.Specs.Support;

namespace Reqnroll.IdeSupport.LSP.Server.Specs.StepDefinitions;

[Binding]
public sealed class NavigationSteps
{
    private readonly LspScenarioContext _ctx;

    public NavigationSteps(LspScenarioContext ctx) => _ctx = ctx;

    [When(@"references are requested at line (\d+) column (\d+) in ""(.*)""")]
    public async Task WhenReferencesAreRequestedAt(int line, int column, string fileName)
    {
        var uri = _ctx.UriFor(fileName);
        _ctx.LastReferences = await _ctx.Harness.Client
            .RequestReferencesAsync(uri, line, column)
            .ConfigureAwait(false);
    }

    [Then(@"(\d+) reference(?:s are|s is| is| are) returned")]
    public void ThenNReferencesAreReturned(int expected)
    {
        if (expected == 0)
        {
            (_ctx.LastReferences is null || !_ctx.LastReferences.Any())
                .Should().BeTrue($"expected 0 references but got {_ctx.LastReferences?.Count()}");
        }
        else
        {
            _ctx.LastReferences.Should().NotBeNull("references should have been returned");
            _ctx.LastReferences!.Count().Should().Be(expected);
        }
    }

    [Then(@"the references include a location in ""(.*)""")]
    public void ThenTheReferencesIncludeALocationIn(string fileName)
    {
        _ctx.LastReferences.Should().NotBeNull("references should have been returned");
        _ctx.LastReferences!.Should().Contain(
            loc => loc.Location!.Uri.ToString()
                       .EndsWith(fileName, StringComparison.OrdinalIgnoreCase),
            $"a reference to '{fileName}' should be present");
    }
}
