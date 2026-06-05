using MediatR;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using Reqnroll.IdeSupport.Common.Diagnostics;
using Reqnroll.IdeSupport.LSP.Core.Diagnostics;
using Reqnroll.IdeSupport.LSP.Core.Document;
using Reqnroll.IdeSupport.LSP.Core.Matching;
using Reqnroll.IdeSupport.LSP.Server.Notifications;
using Reqnroll.IdeSupport.LSP.Server.Services;

namespace Reqnroll.IdeSupport.LSP.Server.Handlers.InternalHandlers;

/// <summary>
/// Handles <see cref="MatchCacheChangedNotification"/> by aggregating the current parse-error
/// tags and binding-mismatch data for the affected feature file and pushing a
/// <c>textDocument/publishDiagnostics</c> notification to the IDE client.
/// </summary>
/// <remarks>
/// Fires alongside <see cref="SemanticTokensRefreshHandler"/> and
/// <see cref="SemanticTokensPushHandler"/> on every <see cref="MatchCacheChangedNotification"/>.
/// No ordering guarantee between these handlers is required — they are independent.
///
/// The LSP specification requires that a single <c>publishDiagnostics</c> message delivers the
/// <em>complete</em> diagnostic set for a URI; sending a partial set clears the diagnostics not
/// included.  The <see cref="IDiagnosticsAggregator"/> combines both sources (parse errors and
/// binding mismatches) into one list before this handler sends the single push.
/// </remarks>
public sealed class DiagnosticsPublishHandler : INotificationHandler<MatchCacheChangedNotification>
{
    private readonly IDocumentBufferService  _documentBufferService;
    private readonly IBindingMatchService    _bindingMatchService;
    private readonly IDiagnosticsAggregator  _aggregator;
    private readonly ILanguageServerFacade   _languageServer;
    private readonly IDeveroomLogger          _logger;

    public DiagnosticsPublishHandler(
        IDocumentBufferService  documentBufferService,
        IBindingMatchService    bindingMatchService,
        IDiagnosticsAggregator  aggregator,
        ILanguageServerFacade   languageServer,
        IDeveroomLogger          logger)
    {
        _documentBufferService = documentBufferService;
        _bindingMatchService   = bindingMatchService;
        _aggregator            = aggregator;
        _languageServer        = languageServer;
        _logger                = logger;
    }

    public Task Handle(MatchCacheChangedNotification notification, CancellationToken cancellationToken)
    {
        var uri = notification.Uri;

        if (!_documentBufferService.TryGet(uri, out var buffer) || buffer?.Tags is null)
        {
            _logger.LogVerbose($"DiagnosticsPublishHandler: no buffer/tags for {uri} — skipping.");
            return Task.CompletedTask;
        }

        _bindingMatchService.TryGet(uri.ToString(), out var matchSet);
        // TryGet returns Empty when not found, so matchSet is never null here.

        var gherkinDiagnostics = _aggregator.Aggregate(buffer.Tags, matchSet);

        var lspDiagnostics = gherkinDiagnostics
            .Select(ToLspDiagnostic)
            .ToArray();

        _logger.LogVerbose(
            $"DiagnosticsPublishHandler: pushing {lspDiagnostics.Length} diagnostic(s) for {uri} v{notification.Version}");

        _languageServer.SendNotification(
            "textDocument/publishDiagnostics",
            new PublishDiagnosticsParams
            {
                Uri         = uri,
                Version     = notification.Version,
                Diagnostics = new Container<Diagnostic>(lspDiagnostics)
            });

        return Task.CompletedTask;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static Diagnostic ToLspDiagnostic(GherkinDiagnostic d)
    {
        var (startLine, startChar) = ResolvePosition(d.Range, d.Range.Start);
        var (endLine,   endChar)   = ResolvePosition(d.Range, d.Range.End);

        return new Diagnostic
        {
            Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(
                new Position(startLine, startChar),
                new Position(endLine,   endChar)),
            Severity = d.Severity == GherkinDiagnosticSeverity.Error
                ? DiagnosticSeverity.Error
                : DiagnosticSeverity.Warning,
            Source  = d.Source,
            Message = d.Message
        };
    }

    /// <summary>
    /// Resolves an absolute character offset to (line, character) using the snapshot embedded
    /// in the <see cref="GherkinRange"/>.  Mirrors the identical helper in
    /// <see cref="SemanticTokenService"/>.
    /// </summary>
    private static (int Line, int Character) ResolvePosition(GherkinRange range, int absoluteOffset)
    {
        var snapshot = range.Snapshot;
        for (int ln = 0; ln < snapshot.LineCount; ln++)
        {
            var line = snapshot.GetLineFromLineNumber(ln);
            if (absoluteOffset <= line.End)
                return (ln, absoluteOffset - line.Start);
        }
        // Clamp to the end of the last line.
        int lastLine = snapshot.LineCount - 1;
        var last = snapshot.GetLineFromLineNumber(lastLine);
        return (lastLine, last.End - last.Start);
    }
}
