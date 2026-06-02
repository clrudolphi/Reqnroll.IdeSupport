using OmniSharp.Extensions.LanguageServer.Protocol;
using Reqnroll.IdeSupport.Common.Diagnostics;
using Reqnroll.IdeSupport.LSP.Core.Discovery;
using Reqnroll.IdeSupport.LSP.Core.Editor.Services.Parsing.GherkinDocuments;
using Reqnroll.IdeSupport.LSP.Server.Discovery;

namespace Reqnroll.IdeSupport.LSP.Server.Services;

public class GherkinDocumentTaggerService : IGherkinDocumentTaggerService
{
    private readonly IDeveroomTagParser _tagParser;
    private readonly IProjectBindingRegistryLookup _registryLookup;
    private readonly ISemanticTokenService _semanticTokenService;
    private readonly IDeveroomLogger _logger;
    private readonly IDocumentBufferService _documentBufferService;

    public GherkinDocumentTaggerService(
        IDocumentBufferService documentBufferService,
        IDeveroomTagParser tagParser,
        IProjectBindingRegistryLookup registryLookup,
        ISemanticTokenService semanticTokenService,
        IDeveroomLogger logger)
    {
        _documentBufferService = documentBufferService;
        _tagParser = tagParser;
        _registryLookup = registryLookup;
        _semanticTokenService = semanticTokenService;
        _logger = logger;
    }

    public Task<IReadOnlyCollection<DeveroomTag>> ParseAsync(DocumentUri uri, int? version)
    {
        if (!_documentBufferService.TryGet(uri, out var buffer))
            return Task.FromResult<IReadOnlyCollection<DeveroomTag>>(Array.Empty<DeveroomTag>());

        var snapshot = buffer?.ToGherkinTextSnapshot();

        if (snapshot == null)
            return Task.FromResult<IReadOnlyCollection<DeveroomTag>>(Array.Empty<DeveroomTag>());

        if (version.HasValue && snapshot.Version != version)
        {
            _logger.LogWarning($"Version mismatch for document {uri}: expected {version}, got {snapshot.Version}");
            return Task.FromResult<IReadOnlyCollection<DeveroomTag>>(Array.Empty<DeveroomTag>());
        }

        // Route to the per-project binding registry for this document URI.
        // Returns ProjectBindingRegistry.Invalid when the project has not yet been
        // discovered or its first discovery run has not completed; DeveroomTagParser
        // gracefully skips step-matching in that case.
        var registry = _registryLookup.GetRegistryForUri(uri);
        var tags = _tagParser.Parse(snapshot, registry);
        _logger.LogInfo($"Parsed {tags.Count} tags from document {uri}");

        // Store the new tags and evict the semantic token cache for this URI.
        // The semantic token cache is keyed on (uri, documentVersion); it must be
        // invalidated here because binding discovery can update the tags for a document
        // whose version has not changed.  Failure to evict would cause VS to receive
        // stale (pre-binding) token data indefinitely.
        _documentBufferService.UpdateTags(uri, tags);
        _semanticTokenService.InvalidateCache(uri);

        return Task.FromResult(tags);
    }
}
