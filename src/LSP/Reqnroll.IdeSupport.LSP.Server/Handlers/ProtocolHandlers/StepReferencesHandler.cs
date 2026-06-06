#nullable enable

using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Reqnroll.IdeSupport.Common.Diagnostics;
using Reqnroll.IdeSupport.LSP.Core.Discovery;
using Reqnroll.IdeSupport.LSP.Core.Matching;
using Reqnroll.IdeSupport.LSP.Server.Document;

namespace Reqnroll.IdeSupport.LSP.Server.Handlers.ProtocolHandlers;

/// <summary>
/// Handles <c>textDocument/references</c> requests originating from a cursor position in a
/// <c>.cs</c> binding file (design doc F14 — Find Step Definition Usages).
/// <para>
/// Converts the cursor position to a <see cref="SourceLocation"/> (file path + 1-based line),
/// queries the binding match cache for every feature-file step that resolves to that location,
/// and returns the results as an array of LSP <see cref="Location"/> objects.
/// </para>
/// </summary>
/// <remarks>
/// Registered via <c>options.OnRequest</c> in <c>Program.cs</c> (same pattern as
/// <see cref="SemanticTokensHandler"/>) to avoid dynamic capability registration and the
/// ambiguity that arises when both the Reqnroll server and the C# server claim
/// <c>textDocument/references</c> for <c>.cs</c> files. See design-doc Q13.
/// </remarks>
public sealed class StepReferencesHandler
{
    private readonly IBindingMatchService _matchService;
    private readonly IDeveroomLogger      _logger;

    public StepReferencesHandler(IBindingMatchService matchService, IDeveroomLogger logger)
    {
        _matchService = matchService;
        _logger       = logger;
    }

    public Task<LocationOrLocationLinks?> Handle(
        ReferenceParams request,
        CancellationToken cancellationToken)
    {
        var uri = request.TextDocument.Uri;

        if (!IsCSharp(uri))
        {
            _logger.LogVerbose($"StepReferencesHandler: ignoring non-.cs URI {uri}");
            return Task.FromResult<LocationOrLocationLinks?>(null);
        }

        var filePath = uri.GetFileSystemPath();
        if (string.IsNullOrEmpty(filePath))
            return Task.FromResult<LocationOrLocationLinks?>(null);

        // LSP positions are 0-based; SourceLocation is 1-based.
        var line   = request.Position.Line + 1;
        var column = request.Position.Character + 1;
        var bindingLocation = new SourceLocation(filePath, line, column);

        var usages = _matchService.FindUsages(bindingLocation);

        if (usages.Count == 0)
        {
            _logger.LogVerbose(
                $"StepReferencesHandler: no usages for binding at {filePath}:{line}");
            return Task.FromResult<LocationOrLocationLinks?>(
                new LocationOrLocationLinks());
        }

        _logger.LogVerbose(
            $"StepReferencesHandler: {usages.Count} usage(s) for binding at {filePath}:{line}");

        var locations = usages
            .Select(match => new LocationOrLocationLink(new Location
            {
                Uri   = DocumentUri.Parse(match.FeatureDocumentId),
                Range = match.Range.ToLspRange()
            }))
            .ToArray();

        return Task.FromResult<LocationOrLocationLinks?>(
            new LocationOrLocationLinks(locations));
    }

    private static bool IsCSharp(DocumentUri uri) =>
        uri.Path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase);
}
