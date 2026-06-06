using MediatR;
using OmniSharp.Extensions.LanguageServer.Protocol;
using Reqnroll.IdeSupport.Common.Diagnostics;
using Reqnroll.IdeSupport.LSP.Server.Notifications;
using Reqnroll.IdeSupport.LSP.Server.Services;

namespace Reqnroll.IdeSupport.LSP.Server.Handlers.InternalHandlers;

/// <summary>
/// Handles <see cref="BindingRegistryChangedNotification"/> by re-parsing feature files
/// that belong to the affected project, then publishing a
/// <see cref="MatchCacheChangedNotification"/> for each open file so that semantic tokens
/// are refreshed against the new binding registry.
/// </summary>
/// <remarks>
/// <para>
/// When <see cref="BindingRegistryChangedNotification.IsFullReplacement"/> is
/// <see langword="true"/> (startup or post-build connector run), all feature files under the
/// project folder are scanned — including files not currently open in the editor — so that the
/// binding match cache is workspace-complete for F14 Find Usages and F18 Code Lens.
/// Closed-file scan entries are stored in <see cref="IBindingMatchService"/> only (not in the
/// document buffer); open-file semantics are unaffected.
/// </para>
/// <para>
/// When <see cref="BindingRegistryChangedNotification.IsFullReplacement"/> is
/// <see langword="false"/> (incremental Roslyn per-file patch on a <c>.cs</c> edit), only the
/// currently open feature files are re-parsed, keeping the hot path cheap.
/// </para>
/// </remarks>
public class BindingRegistryChangedHandler : INotificationHandler<BindingRegistryChangedNotification>
{
    private readonly IDocumentBufferService        _documentBufferService;
    private readonly IGherkinDocumentTaggerService  _taggerService;
    private readonly IMediator                     _mediator;
    private readonly IDeveroomLogger                _logger;

    public BindingRegistryChangedHandler(
        IDocumentBufferService documentBufferService,
        IGherkinDocumentTaggerService taggerService,
        IMediator mediator,
        IDeveroomLogger logger)
    {
        _documentBufferService = documentBufferService;
        _taggerService         = taggerService;
        _mediator              = mediator;
        _logger                = logger;
    }

    public async Task Handle(
        BindingRegistryChangedNotification notification,
        CancellationToken cancellationToken)
    {
        var projectFolder = notification.Project.ProjectFolder;

        if (notification.IsFullReplacement)
            await ScanAllFeatureFilesAsync(projectFolder, cancellationToken).ConfigureAwait(false);

        await ReparseOpenFilesAsync(projectFolder, cancellationToken).ConfigureAwait(false);
    }

    private async Task ScanAllFeatureFilesAsync(string projectFolder, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(projectFolder) || !Directory.Exists(projectFolder))
            return;

        var allFeatureFiles = Directory
            .EnumerateFiles(projectFolder, "*.feature", SearchOption.AllDirectories)
            .ToList();

        // Skip files that are already open — ReparseOpenFilesAsync will handle those via
        // the buffer and publish MatchCacheChangedNotification for semantic token refresh.
        var openUris = _documentBufferService.All
            .Select(b => b.Uri.GetFileSystemPath())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var closedFiles = allFeatureFiles
            .Where(f => !openUris.Contains(f))
            .ToList();

        _logger.LogInfo(
            $"Full registry replacement — scanning {closedFiles.Count} closed feature file(s) " +
            $"under '{projectFolder}'.");

        foreach (var filePath in closedFiles)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var text = await File.ReadAllTextAsync(filePath, cancellationToken).ConfigureAwait(false);
                var uri  = DocumentUri.FromFileSystemPath(filePath);
                await _taggerService.ScanClosedFileAsync(uri, text).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning($"ScanAllFeatureFiles: could not scan '{filePath}': {ex.Message}");
            }
        }
    }

    private async Task ReparseOpenFilesAsync(string projectFolder, CancellationToken cancellationToken)
    {
        var affectedBuffers = _documentBufferService.All
            .Where(b => IsUnderProjectFolder(b.Uri, projectFolder))
            .ToList();

        if (affectedBuffers.Count == 0)
        {
            _logger.LogVerbose(
                $"BindingRegistryChanged — no open feature files to reparse under '{projectFolder}'.");
            return;
        }

        _logger.LogInfo(
            $"BindingRegistryChanged — reparsing {affectedBuffers.Count} open feature file(s) " +
            $"under '{projectFolder}'.");

        foreach (var buffer in affectedBuffers)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await ParseAndNotifyAsync(buffer.Uri, buffer.Version, cancellationToken)
                .ConfigureAwait(false);
        }
    }

    private async Task ParseAndNotifyAsync(
        DocumentUri uri,
        int? version,
        CancellationToken cancellationToken)
    {
        // ParseAsync stores updated tags, recomputes/stores the binding match set, and
        // invalidates the semantic token cache internally before this notification fires.
        await _taggerService.ParseAsync(uri, version).ConfigureAwait(false);
        await _mediator.Publish(
            new MatchCacheChangedNotification(uri, version ?? 0),
            cancellationToken).ConfigureAwait(false);
    }

    private static bool IsUnderProjectFolder(DocumentUri uri, string projectFolder)
    {
        var filePath = uri.GetFileSystemPath();
        if (string.IsNullOrEmpty(filePath) || string.IsNullOrEmpty(projectFolder))
            return false;

        return filePath.StartsWith(projectFolder, StringComparison.OrdinalIgnoreCase);
    }
}
