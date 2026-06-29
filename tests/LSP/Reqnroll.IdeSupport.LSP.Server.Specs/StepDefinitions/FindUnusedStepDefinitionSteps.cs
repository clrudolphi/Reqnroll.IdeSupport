using AwesomeAssertions;
using Reqnroll;
using Reqnroll.IdeSupport.LSP.Server.Specs.Support;

namespace Reqnroll.IdeSupport.LSP.Server.Specs.StepDefinitions;

[Binding]
public sealed class FindUnusedStepDefinitionSteps
{
    private readonly LspScenarioContext _ctx;

    public FindUnusedStepDefinitionSteps(LspScenarioContext ctx) => _ctx = ctx;

    // ── When ───────────────────────────────────────────────────────────────────

    [When("unused step definitions are requested")]
    public async Task WhenUnusedStepDefinitionsAreRequested()
    {
        await _ctx.EnsureStartedAsync().ConfigureAwait(false);
        _ctx.LastFindUnused = await _ctx.Harness.Client
            .RequestFindUnusedStepDefinitionsAsync()
            .ConfigureAwait(false);
    }

    // ── Then ───────────────────────────────────────────────────────────────────

    [Then(@"(\d+) unused step definition(?:s are|s is| is| are) returned")]
    public void ThenNUnusedStepDefinitionsAreReturned(int expected)
    {
        var count = _ctx.LastFindUnused?.Items?.Count ?? 0;
        count.Should().Be(expected, $"expected {expected} unused step definition(s) but got {count}");
    }

    [Then(@"the unused step definitions include expression ""(.*)""")]
    public void ThenUnusedStepDefinitionsIncludeExpression(string expression)
    {
        _ctx.LastFindUnused.Should().NotBeNull();
        _ctx.LastFindUnused!.Items.Should().Contain(
            item => item.BindingExpression == expression,
            $"an unused step definition with expression '{expression}' should be present");
    }

    [Then(@"the unused step definitions do not include expression ""(.*)""")]
    public void ThenUnusedStepDefinitionsDoNotIncludeExpression(string expression)
    {
        if (_ctx.LastFindUnused is null) return;
        _ctx.LastFindUnused.Items.Should().NotContain(
            item => item.BindingExpression == expression,
            $"expression '{expression}' should not appear in the unused step definitions");
    }
}
