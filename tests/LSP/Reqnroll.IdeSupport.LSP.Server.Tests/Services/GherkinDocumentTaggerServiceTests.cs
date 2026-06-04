using Reqnroll.IdeSupport.Common.Diagnostics;
using Reqnroll.IdeSupport.LSP.Core.Discovery;
using Reqnroll.IdeSupport.LSP.Core.Editor.Services.Parsing.GherkinDocuments;
using Reqnroll.IdeSupport.LSP.Core.Matching;
using Reqnroll.IdeSupport.LSP.Server.Discovery;
using Reqnroll.IdeSupport.LSP.Server.Services;

namespace Reqnroll.IdeSupport.LSP.Server.Tests.Services;

public class GherkinDocumentTaggerServiceTests
{
    private readonly IDocumentBufferService _bufferService = Substitute.For<IDocumentBufferService>();
    private readonly IDeveroomTagParser _tagParser = Substitute.For<IDeveroomTagParser>();
    private readonly IProjectBindingRegistryLookup _registryLookup = Substitute.For<IProjectBindingRegistryLookup>();
    private readonly ISemanticTokenService _semanticTokenService = Substitute.For<ISemanticTokenService>();
    private readonly IBindingMatchService _bindingMatchService = Substitute.For<IBindingMatchService>();
    private readonly IDeveroomLogger _logger = Substitute.For<IDeveroomLogger>();

    private static readonly DocumentUri FeatureUri = DocumentUri.FromFileSystemPath("/workspace/test.feature");

    public GherkinDocumentTaggerServiceTests()
    {
        // Default: no project registered for this URI, so Invalid is returned.
        _registryLookup.GetRegistryForUri(Arg.Any<DocumentUri>())
                       .Returns(ProjectBindingRegistry.Invalid);
    }

    private GherkinDocumentTaggerService CreateSut() =>
        new(_bufferService, _tagParser, _registryLookup, _semanticTokenService, _bindingMatchService, _logger);

    // ── Buffer-not-found ──────────────────────────────────────────────────────

    [Fact]
    public async Task ParseAsync_returns_empty_when_buffer_not_registered()
    {
        DocumentBuffer? ignored;
        _bufferService.TryGet(FeatureUri, out ignored).Returns(x =>
        {
            x[1] = (DocumentBuffer?)null;
            return false;
        });

        var sut = CreateSut();
        var result = await sut.ParseAsync(FeatureUri, version: 1);
        result.Should().BeEmpty();
    }

    // ── Version mismatch ──────────────────────────────────────────────────────

    [Fact]
    public async Task ParseAsync_returns_empty_when_version_does_not_match()
    {
        var buf = new DocumentBuffer(FeatureUri, 1, "Feature: X\n");
        DocumentBuffer? ignored2;
        _bufferService.TryGet(FeatureUri, out ignored2).Returns(x =>
        {
            x[1] = buf;
            return true;
        });

        var sut = CreateSut();
        // Request version 99 but buffer has version 1
        var result = await sut.ParseAsync(FeatureUri, version: 99);
        result.Should().BeEmpty();
        // Version mismatch is logged via extension method (not verifiable via NSubstitute directly)
    }

    // ── Successful parse ──────────────────────────────────────────────────────

    [Fact]
    public async Task ParseAsync_invokes_tag_parser_and_returns_tags()
    {
        var buf = new DocumentBuffer(FeatureUri, 5, "Feature: X\n");
        DocumentBuffer? ignored4;
        _bufferService.TryGet(FeatureUri, out ignored4).Returns(x =>
        {
            x[1] = buf;
            return true;
        });

        var expectedTags = (IReadOnlyCollection<DeveroomTag>)Array.Empty<DeveroomTag>();
        _tagParser.Parse(
                      Arg.Any<Reqnroll.IdeSupport.LSP.Core.Document.IGherkinTextSnapshot>(),
                      Arg.Any<ProjectBindingRegistry>())
                  .Returns(expectedTags);

        var sut = CreateSut();
        var result = await sut.ParseAsync(FeatureUri, version: 5);
        result.Should().BeSameAs(expectedTags);
        _tagParser.Received(1).Parse(
            Arg.Any<Reqnroll.IdeSupport.LSP.Core.Document.IGherkinTextSnapshot>(),
            Arg.Any<ProjectBindingRegistry>());
        // The match set is recomputed and stored against the document URI before the
        // semantic token cache is evicted.
        _bindingMatchService.Received(1).Store(
            Arg.Is<FeatureBindingMatchSet>(s => s.DocumentId == FeatureUri.ToString()));
        _semanticTokenService.Received(1).InvalidateCache(FeatureUri);
    }

    [Fact]
    public async Task ParseAsync_passes_when_no_version_specified()
    {
        var buf = new DocumentBuffer(FeatureUri, 5, "Feature: X\n");
        DocumentBuffer? ignored5;
        _bufferService.TryGet(FeatureUri, out ignored5).Returns(x =>
        {
            x[1] = buf;
            return true;
        });

        var expectedTags = (IReadOnlyCollection<DeveroomTag>)Array.Empty<DeveroomTag>();
        _tagParser.Parse(
                      Arg.Any<Reqnroll.IdeSupport.LSP.Core.Document.IGherkinTextSnapshot>(),
                      Arg.Any<ProjectBindingRegistry>())
                  .Returns(expectedTags);

        var sut = CreateSut();
        var result = await sut.ParseAsync(FeatureUri, version: null);
        result.Should().BeSameAs(expectedTags);
    }
}
