#nullable enable

using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using OmniSharp.Extensions.LanguageServer.Protocol.Workspace;
using Reqnroll.IdeSupport.Common.Logging;
using Reqnroll.IdeSupport.LSP.Server.Pipeline;

namespace Reqnroll.IdeSupport.LSP.Server.Tests.Pipeline;

public class InlayHintRefreshHandlerTests
{
    private readonly ILanguageServerFacade _languageServer = Substitute.For<ILanguageServerFacade>();
    private readonly IIdeSupportLogger _logger = Substitute.For<IIdeSupportLogger>();

    private InlayHintRefreshHandler CreateSut() => new(_languageServer, _logger);

    private void SetRefreshSupport(bool? supported)
    {
        _languageServer.ClientSettings.Returns(new InitializeParams
        {
            Capabilities = new ClientCapabilities
            {
                Workspace = new WorkspaceClientCapabilities
                {
                    InlayHint = supported.HasValue
                        ? new Supports<InlayHintWorkspaceClientCapabilities>(true,
                            new InlayHintWorkspaceClientCapabilities { RefreshSupport = supported.Value })
                        : new Supports<InlayHintWorkspaceClientCapabilities>(false)
                }
            }
        });
    }

    [Fact]
    public async Task Handle_sends_refresh_when_the_client_supports_it()
    {
        SetRefreshSupport(true);

        await CreateSut().Handle(new MatchCacheChangedNotification(DocumentUri.From("file:///f.feature"), 1), CancellationToken.None);
        await Task.Delay(700); // past the 500ms debounce window

        _languageServer.Client.Received(1).SendRequest(WorkspaceNames.InlayHintRefresh);
    }

    [Fact]
    public async Task Handle_does_not_send_refresh_when_the_client_declares_no_support()
    {
        SetRefreshSupport(false);

        await CreateSut().Handle(new MatchCacheChangedNotification(DocumentUri.From("file:///f.feature"), 1), CancellationToken.None);
        await Task.Delay(700);

        _languageServer.Client.DidNotReceive().SendRequest(WorkspaceNames.InlayHintRefresh);
    }

    [Fact]
    public async Task Handle_does_not_send_refresh_when_workspace_capabilities_are_absent()
    {
        _languageServer.ClientSettings.Returns(new InitializeParams { Capabilities = new ClientCapabilities() });

        await CreateSut().Handle(new MatchCacheChangedNotification(DocumentUri.From("file:///f.feature"), 1), CancellationToken.None);
        await Task.Delay(700);

        _languageServer.Client.DidNotReceive().SendRequest(WorkspaceNames.InlayHintRefresh);
    }

    [Fact]
    public async Task Handle_debounces_bursts_into_a_single_refresh()
    {
        SetRefreshSupport(true);

        var uri = DocumentUri.From("file:///f.feature");
        var sut = CreateSut();
        await sut.Handle(new MatchCacheChangedNotification(uri, 1), CancellationToken.None);
        await sut.Handle(new MatchCacheChangedNotification(uri, 2), CancellationToken.None);
        await sut.Handle(new MatchCacheChangedNotification(uri, 3), CancellationToken.None);
        await Task.Delay(700);

        _languageServer.Client.Received(1).SendRequest(WorkspaceNames.InlayHintRefresh);
    }
}
