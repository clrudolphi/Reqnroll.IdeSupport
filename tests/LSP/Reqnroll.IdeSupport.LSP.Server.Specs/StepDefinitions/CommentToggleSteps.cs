using AwesomeAssertions;
using Newtonsoft.Json.Linq;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Reqnroll;
using Reqnroll.IdeSupport.LSP.Server.Specs.Support;

namespace Reqnroll.IdeSupport.LSP.Server.Specs.StepDefinitions;

[Binding]
public sealed class CommentToggleSteps
{
    private readonly LspScenarioContext _ctx;

    public CommentToggleSteps(LspScenarioContext ctx) => _ctx = ctx;

    // -- When ------------------------------------------------------------------

    [When("the toggle comment command is executed for \"(.*)\" on lines (\\d+) to (\\d+)")]
    public async Task WhenTheToggleCommentCommandIsExecuted(string fileName, int startLine, int endLine)
    {
        await _ctx.EnsureStartedAsync().ConfigureAwait(false);
        var uri = _ctx.UriFor(fileName);
        _ctx.LastToggleEdit = null;

        await _ctx.Harness.Client.RequestCommandAsync(new ExecuteCommandParams
        {
            Command = "reqnroll.toggleComment",
            Arguments = new JArray(uri.ToString(), startLine, endLine)
        }).ConfigureAwait(false);

        _ctx.LastToggleEdit = _ctx.Harness.LastApplyEdit;
    }

    // -- Then ------------------------------------------------------------------

    [Then("a workspace\\/applyEdit notification is sent")]
    public void ThenAWorkspaceApplyEditNotificationIsSent()
    {
        _ctx.LastToggleEdit.Should().NotBeNull("the server should send workspace/applyEdit");
    }

    [Then("the edit replaces line (\\d+) with \"(.*)\"")]
    public void ThenTheEditReplacesLineWith(int line, string expectedText)
    {
        var edit = _ctx.LastToggleEdit;
        edit.Should().NotBeNull();

        var docChanges = edit!.Edit.DocumentChanges;
        docChanges.Should().NotBeNull();
        var docEdit = docChanges!.First().TextDocumentEdit;
        docEdit.Should().NotBeNull("the edit should contain a TextDocumentEdit");

        var textEdit = docEdit!.Edits.Should().ContainSingle(
            e => e.Range.Start.Line == line && e.Range.End.Line == line,
            $"a text edit for line {line} should exist").Subject;
        textEdit.NewText.Should().Be(expectedText);
    }
}
