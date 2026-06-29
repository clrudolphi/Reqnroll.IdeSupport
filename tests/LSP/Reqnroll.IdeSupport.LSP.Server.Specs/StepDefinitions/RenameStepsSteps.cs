using AwesomeAssertions;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Reqnroll;
using Reqnroll.IdeSupport.LSP.Server.Specs.Support;

namespace Reqnroll.IdeSupport.LSP.Server.Specs.StepDefinitions;

[Binding]
public sealed class RenameStepsSteps
{
    private readonly LspScenarioContext _ctx;

    public RenameStepsSteps(LspScenarioContext ctx) => _ctx = ctx;

    // ── When: rename requests ──────────────────────────────────────────────────

    [When(@"rename is requested at line (\d+) column (\d+) in ""(.*)"" with new name ""(.*)""")]
    public async Task WhenRenameIsRequested(int line, int column, string fileName, string newName)
    {
        var uri = _ctx.UriFor(fileName);
        _ctx.LastRenameEdit = await _ctx.Harness.Client
            .RequestRenameAsync(uri, line, column, newName)
            .ConfigureAwait(false);
    }

    [When(@"prepare rename is requested at line (\d+) column (\d+) in ""(.*)""")]
    public async Task WhenPrepareRenameIsRequested(int line, int column, string fileName)
    {
        var uri = _ctx.UriFor(fileName);
        try
        {
            _ctx.LastPrepareRenameRange = await _ctx.Harness.Client
                .RequestPrepareRenameAsync(uri, line, column)
                .ConfigureAwait(false);
        }
        catch
        {
            // OmniSharp returns an error response (not null) when the handler returns null
            // for nullable request types. Treat any error as "no range returned".
            _ctx.LastPrepareRenameRange = null;
        }
    }

    [When(@"rename targets are requested at line (\d+) column (\d+) in ""(.*)""")]
    public async Task WhenRenameTargetsAreRequestedAt(int line, int column, string fileName)
    {
        var uri = _ctx.UriFor(fileName);
        _ctx.LastRenameTargets = await _ctx.Harness.Client
            .RequestRenameTargetsAsync(uri, line, column)
            .ConfigureAwait(false);
    }

    // ── Then: rename edit assertions ───────────────────────────────────────────

    [Then("a workspace edit is returned")]
    public void ThenAWorkspaceEditIsReturned()
    {
        _ctx.LastRenameEdit.Should().NotBeNull("the server should return a WorkspaceEdit for a valid rename");
        _ctx.LastRenameEdit!.Changes.Should().NotBeNullOrEmpty("the workspace edit should contain at least one file change");
    }

    [Then("no workspace edit is returned")]
    public void ThenNoWorkspaceEditIsReturned()
    {
        (_ctx.LastRenameEdit is null || _ctx.LastRenameEdit.Changes is null || !_ctx.LastRenameEdit.Changes.Any())
            .Should().BeTrue("the server should return null or an empty WorkspaceEdit for an invalid rename position");
    }

    [Then("no prepare rename range is returned")]
    public void ThenNoPrepareRenameRangeIsReturned()
    {
        _ctx.LastPrepareRenameRange.Should().BeNull(
            "prepareRename should return null when the cursor is not on a step binding");
    }

    [Then(@"the workspace edit contains a change in ""(.*)""")]
    public void ThenWorkspaceEditContainsChangeIn(string fileName)
    {
        _ctx.LastRenameEdit.Should().NotBeNull();
        var changes = _ctx.LastRenameEdit!.Changes;
        changes.Should().NotBeNull($"WorkspaceEdit.Changes should be populated for '{fileName}'");
        changes!.Keys.Should().Contain(
            k => k.ToString().EndsWith(fileName, StringComparison.OrdinalIgnoreCase),
            $"the workspace edit should include changes to '{fileName}'");
    }

    [Then(@"the workspace edit changes to ""(.*)"" include new text ""(.*)""")]
    public void ThenWorkspaceEditChangesIncludeNewText(string fileName, string expectedText)
    {
        _ctx.LastRenameEdit.Should().NotBeNull();
        var changes = _ctx.LastRenameEdit!.Changes;
        changes.Should().NotBeNull($"WorkspaceEdit.Changes should be populated for '{fileName}'");
        var edits = changes!
            .Where(kvp => kvp.Key.ToString().EndsWith(fileName, StringComparison.OrdinalIgnoreCase))
            .SelectMany(kvp => kvp.Value)
            .ToList();

        edits.Should().Contain(
            e => e.NewText.Contains(expectedText, StringComparison.OrdinalIgnoreCase),
            $"a text edit for '{fileName}' should contain '{expectedText}'");
    }

    // ── Then: rename targets assertions ────────────────────────────────────────

    [Then(@"(\d+) rename target(?:s are|s is| is| are) returned")]
    public void ThenNRenameTargetsAreReturned(int expected)
    {
        var count = _ctx.LastRenameTargets?.Targets?.Count ?? 0;
        count.Should().Be(expected, $"expected {expected} rename target(s) but got {count}");
    }

    [Then(@"the rename targets include expression ""(.*)""")]
    public void ThenRenameTargetsIncludeExpression(string expression)
    {
        _ctx.LastRenameTargets.Should().NotBeNull();
        _ctx.LastRenameTargets!.Targets.Should().Contain(
            t => t.Expression == expression,
            $"a rename target with expression '{expression}' should be present");
    }
}
