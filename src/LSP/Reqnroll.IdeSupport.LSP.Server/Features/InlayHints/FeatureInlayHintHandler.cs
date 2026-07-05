#nullable enable

using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Reqnroll.IdeSupport.Common.Diagnostics;
using Reqnroll.IdeSupport.LSP.Core.InlayHints;
using Reqnroll.IdeSupport.LSP.Core.Matching;
using Reqnroll.IdeSupport.LSP.Server.Documents;
using Reqnroll.IdeSupport.LSP.Server.Workspace;
using LspRange = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;

namespace Reqnroll.IdeSupport.LSP.Server.Features.InlayHints;

/// <summary>
/// Handles <c>textDocument/inlayHint</c> for <c>.feature</c> files (F23 — binding info hints).
/// Shows the bound step definition's method name at the end of each step line, with the full
/// signature in the hint's tooltip. Ambiguous steps show a match count instead; a Scenario
/// Outline/Background step whose example rows resolve to more than one distinct binding shows a
/// binding count. Undefined steps get no hint — the diagnostic already covers those.
/// </summary>
public sealed class FeatureInlayHintHandler : IInlayHintsHandler
{
    private readonly IBindingMatchService      _matchService;
    private readonly ILspWorkspaceScopeManager _scopeManager;
    private readonly IGherkinInlayHintService  _hintService;
    private readonly IDeveroomLogger           _logger;

    private static readonly TextDocumentSelector FeatureSelector = new(
        new TextDocumentFilter { Pattern = "**/*.feature" });

    public FeatureInlayHintHandler(
        IBindingMatchService      matchService,
        ILspWorkspaceScopeManager scopeManager,
        IGherkinInlayHintService  hintService,
        IDeveroomLogger           logger)
    {
        _matchService = matchService;
        _scopeManager = scopeManager;
        _hintService  = hintService;
        _logger       = logger;
    }

    public InlayHintRegistrationOptions GetRegistrationOptions(
        InlayHintClientCapabilities capability, ClientCapabilities clientCapabilities)
        => new() { DocumentSelector = FeatureSelector, ResolveProvider = false };

    public Task<InlayHintContainer?> Handle(InlayHintParams request, CancellationToken cancellationToken)
    {
        var uri = request.TextDocument.Uri;

        var primaryOwner = _scopeManager.ResolvePrimaryOwner(uri);
        var matchKey = primaryOwner is not null
            ? new MatchSetKey(uri.ToString(), new ProjectOwner(primaryOwner.ProjectFullName, primaryOwner.TargetFrameworkMoniker))
            : MatchSetKey.ForUnknownProject(uri.ToString());

        if (!_matchService.TryGet(matchKey, out var matchSet) || matchSet is null)
        {
            _logger.LogVerbose($"FeatureInlayHintHandler: no match set cached for {uri}");
            return Task.FromResult<InlayHintContainer?>(new InlayHintContainer());
        }

        var hints = _hintService.Build(matchSet)
            .Select(ToInlayHint)
            .Where(h => Intersects(h.Position, request.Range))
            .ToList();

        _logger.LogVerbose($"FeatureInlayHintHandler: {hints.Count} hint(s) for {uri}");
        return Task.FromResult<InlayHintContainer?>(new InlayHintContainer(hints));
    }

    private static InlayHint ToInlayHint(GherkinInlayHint hint)
    {
        var position = hint.AnchorRange.ToLspRange().End;
        return new InlayHint
        {
            Position     = position,
            // The implicit string conversion returns StringOrInlayHintLabelParts? (it also
            // accepts null input), which trips a nullable warning against this non-nullable
            // property; the explicit constructor sidesteps that.
            Label        = new StringOrInlayHintLabelParts(hint.Label),
            Kind         = InlayHintKind.Type,
            Tooltip      = hint.Tooltip,
            PaddingLeft  = true,
        };
    }

    /// <summary>Whether the (single-point) hint position falls within the requested viewport.</summary>
    private static bool Intersects(Position position, LspRange range)
    {
        var afterStart = position.Line > range.Start.Line ||
            (position.Line == range.Start.Line && position.Character >= range.Start.Character);
        var beforeEnd = position.Line < range.End.Line ||
            (position.Line == range.End.Line && position.Character <= range.End.Character);
        return afterStart && beforeEnd;
    }
}
