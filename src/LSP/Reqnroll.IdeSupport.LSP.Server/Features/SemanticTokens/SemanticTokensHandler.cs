using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Reqnroll.IdeSupport.Common.Diagnostics;
using Reqnroll.IdeSupport.LSP.Server.Protocol;
using Reqnroll.IdeSupport.LSP.Server.Features.TextSync;

using LspSemanticTokens = OmniSharp.Extensions.LanguageServer.Protocol.Models.SemanticTokens;
using LspSemanticTokensFullOrDelta = OmniSharp.Extensions.LanguageServer.Protocol.Models.SemanticTokensFullOrDelta;

namespace Reqnroll.IdeSupport.LSP.Server.Features.SemanticTokens;

/// <summary>
/// Handles <c>textDocument/semanticTokens/full</c>, <c>textDocument/semanticTokens/full/delta</c>,
/// and <c>textDocument/semanticTokens/range</c> requests by delegating to <see cref="ISemanticTokenService"/>.
/// </summary>
public class SemanticTokensHandler
{
    // OmniSharp's DelegatingRequestHandler serialises the response with JToken.FromObject(),
    // which throws ArgumentNullException when passed null — even though LSP allows null.
    // Return this instead of null whenever the service has no tokens yet.
    private static readonly LspSemanticTokens EmptyTokens = new() { Data = [] };

    private readonly ISemanticTokenService _semanticTokenService;
    private readonly IDocumentBufferService _documentBufferService;
    private readonly IIdeSupportLogger _logger;

    public SemanticTokensHandler(
        ISemanticTokenService semanticTokenService,
        IDocumentBufferService documentBufferService,
        IIdeSupportLogger logger)
    {
        _semanticTokenService = semanticTokenService;
        _documentBufferService = documentBufferService;
        _logger = logger;
    }

    // ── Full ──────────────────────────────────────────────────────────────────

    public async Task<LspSemanticTokens> HandleAsync(
        SemanticTokensParams request,
        CancellationToken cancellationToken)
    {
        var uri = request.TextDocument.Uri;
        if (!IsFeatureFile(uri)) return EmptyTokens;
        var version = GetCurrentVersion(uri);

        _logger.LogVerbose($"SemanticTokens/full requested for {uri} (version {version})");

        return await _semanticTokenService.GetSemanticTokensAsync(uri, version, cancellationToken)
                                          .ConfigureAwait(false)
               ?? EmptyTokens;
    }

    // ── Delta ─────────────────────────────────────────────────────────────────
    // We don't maintain delta state; return the full token set wrapped in SemanticTokensFullOrDelta.

    public async Task<LspSemanticTokensFullOrDelta> HandleAsync(
        SemanticTokensDeltaParams request,
        CancellationToken cancellationToken)
    {
        var uri = request.TextDocument.Uri;
        if (!IsFeatureFile(uri)) return new LspSemanticTokensFullOrDelta(EmptyTokens);
        var version = GetCurrentVersion(uri);

        _logger.LogVerbose($"SemanticTokens/full/delta requested for {uri} (version {version}), returning full tokens");

        var tokens = await _semanticTokenService.GetSemanticTokensAsync(uri, version, cancellationToken)
                                                .ConfigureAwait(false);

        return new LspSemanticTokensFullOrDelta(tokens ?? EmptyTokens);
    }

    // ── Range ─────────────────────────────────────────────────────────────────
    // Return all tokens; the client will filter by range.

    public async Task<LspSemanticTokens> HandleAsync(
        SemanticTokensRangeParams request,
        CancellationToken cancellationToken)
    {
        var uri = request.TextDocument.Uri;
        if (!IsFeatureFile(uri)) return EmptyTokens;
        var version = GetCurrentVersion(uri);

        _logger.LogVerbose($"SemanticTokens/range requested for {uri} (version {version})");

        return await _semanticTokenService.GetSemanticTokensAsync(uri, version, cancellationToken)
                                          .ConfigureAwait(false)
               ?? EmptyTokens;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static bool IsFeatureFile(OmniSharp.Extensions.LanguageServer.Protocol.DocumentUri uri)
        => uri.Path.EndsWith(".feature", StringComparison.OrdinalIgnoreCase);

    private int GetCurrentVersion(OmniSharp.Extensions.LanguageServer.Protocol.DocumentUri uri)
    {
        if (_documentBufferService.TryGet(uri, out var buffer) && buffer?.Version is int v)
            return v;

        return 0;
    }
}
