using MediatR;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Reqnroll.IdeSupport.Common.Diagnostics;
using Reqnroll.IdeSupport.LSP.Core.Editor.Services.Parsing.GherkinDocuments;
using Reqnroll.IdeSupport.LSP.Core.Matching;
using Reqnroll.IdeSupport.LSP.Server.Handlers.ProtocolHandlers;
using Reqnroll.IdeSupport.LSP.Server.Notifications;
using Reqnroll.IdeSupport.LSP.Server.Services;

namespace Reqnroll.IdeSupport.LSP.Server.Tests.Handlers;

public class TextDocumentSyncHandlerTests
{
    private readonly IDocumentBufferService _bufferService = new DocumentBufferService();
    private readonly IGherkinDocumentTaggerService _taggerService = Substitute.For<IGherkinDocumentTaggerService>();
    private readonly IBindingMatchService _bindingMatchService = Substitute.For<IBindingMatchService>();
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly IDeveroomLogger _logger = Substitute.For<IDeveroomLogger>();

    private static readonly DocumentUri FeatureUri = DocumentUri.FromFileSystemPath("/workspace/test.feature");

    private TextDocumentSyncHandler CreateSut() =>
        new(_bufferService, _taggerService, _bindingMatchService, _mediator, _logger);

    [Fact]
    public async Task Handle_DidOpen_stores_document_and_publishes_match_cache_changed_notification()
    {
        var tags = Array.Empty<DeveroomTag>();
        _taggerService.ParseAsync(FeatureUri, 3).Returns(tags);

        var sut = CreateSut();
        var request = new DidOpenTextDocumentParams
        {
            TextDocument = new TextDocumentItem
            {
                Uri = FeatureUri,
                Version = 3,
                Text = "Feature: Opened\nScenario: S\n  Given step\n",
                LanguageId = "gherkin"
            }
        };

        var result = await sut.Handle(request, CancellationToken.None);

        result.Should().Be(Unit.Value);

        _bufferService.TryGet(FeatureUri, out var buffer).Should().BeTrue();
        buffer.Should().NotBeNull();
        buffer!.Text.Should().Be(request.TextDocument.Text);
        buffer.Version.Should().Be(3);
        // Tag storage and match-set computation are delegated to IGherkinDocumentTaggerService.ParseAsync
        // (mocked here); the handler only publishes the match-cache-changed notification afterwards.

        await _mediator.Received(1).Publish(
            Arg.Is<MatchCacheChangedNotification>(n => n.Uri == FeatureUri && n.Version == 3),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DidChange_updates_document_text_and_version_then_publishes()
    {
        _bufferService.Update(FeatureUri, 1, "Feature: Old\n");

        var tags = Array.Empty<DeveroomTag>();
        _taggerService.ParseAsync(FeatureUri, 2).Returns(tags);

        var sut = CreateSut();
        var request = new DidChangeTextDocumentParams
        {
            TextDocument = new OptionalVersionedTextDocumentIdentifier
            {
                Uri = FeatureUri,
                Version = 2
            },
            ContentChanges = new Container<TextDocumentContentChangeEvent>(
                new TextDocumentContentChangeEvent { Text = "Feature: New\nScenario: S\n  Given changed\n" })
        };

        var result = await sut.Handle(request, CancellationToken.None);

        result.Should().Be(Unit.Value);

        _bufferService.TryGet(FeatureUri, out var buffer).Should().BeTrue();
        buffer.Should().NotBeNull();
        buffer!.Text.Should().Be("Feature: New\nScenario: S\n  Given changed\n");
        buffer.Version.Should().Be(2);
        // Tag storage and match-set computation are delegated to IGherkinDocumentTaggerService.ParseAsync
        // (mocked here); the handler only publishes the match-cache-changed notification afterwards.

        await _mediator.Received(1).Publish(
            Arg.Is<MatchCacheChangedNotification>(n => n.Uri == FeatureUri && n.Version == 2),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DidChange_with_empty_changes_uses_empty_string_and_publishes()
    {
        var tags = Array.Empty<DeveroomTag>();
        _taggerService.ParseAsync(FeatureUri, 4).Returns(tags);

        var sut = CreateSut();
        var request = new DidChangeTextDocumentParams
        {
            TextDocument = new OptionalVersionedTextDocumentIdentifier
            {
                Uri = FeatureUri,
                Version = 4
            },
            ContentChanges = new Container<TextDocumentContentChangeEvent>()
        };

        await sut.Handle(request, CancellationToken.None);

        _bufferService.TryGet(FeatureUri, out var buffer).Should().BeTrue();
        buffer!.Text.Should().Be(string.Empty);
        buffer.Version.Should().Be(4);

        await _mediator.Received(1).Publish(
            Arg.Is<MatchCacheChangedNotification>(n => n.Uri == FeatureUri && n.Version == 4),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DidClose_removes_document_buffer()
    {
        _bufferService.Update(FeatureUri, 1, "Feature: X\n");

        var sut = CreateSut();
        var request = new DidCloseTextDocumentParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = FeatureUri }
        };

        var result = await sut.Handle(request, CancellationToken.None);

        result.Should().Be(Unit.Value);
        _bufferService.TryGet(FeatureUri, out _).Should().BeFalse();
        _bindingMatchService.Received(1).Invalidate(FeatureUri.ToString());

        await _mediator.DidNotReceiveWithAnyArgs().Publish(default!, default);
    }

    [Fact]
    public async Task Handle_DidSave_does_not_modify_buffer_or_publish_notification()
    {
        _bufferService.Update(FeatureUri, 7, "Feature: BeforeSave\n");

        var sut = CreateSut();
        var request = new DidSaveTextDocumentParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = FeatureUri }
        };

        var result = await sut.Handle(request, CancellationToken.None);

        result.Should().Be(Unit.Value);
        _bufferService.TryGet(FeatureUri, out var buffer).Should().BeTrue();
        buffer!.Version.Should().Be(7);
        buffer.Text.Should().Be("Feature: BeforeSave\n");

        await _mediator.DidNotReceiveWithAnyArgs().Publish(default!, default);
    }
}
