using MediatR;
using OmniSharp.Extensions.LanguageServer.Protocol;
using Reqnroll.IdeSupport.Common.Diagnostics;
using Reqnroll.IdeSupport.LSP.Server.Notifications;
using Reqnroll.IdeSupport.LSP.Server.Services;

namespace Reqnroll.IdeSupport.LSP.Server.Handlers.InternalHandlers;

/// <summary>
/// Handles <see cref="BindingRegistryChangedNotification"/> by re-parsing every open
/// feature file that belongs to the affected project, then publishing a
/// <see cref="GherkinDocumentParsedNotification"/> for each so that semantic tokens
/// are refreshed against the new binding registry.
/// </summary>
/// <remarks>
/// This handler is the binding-discovery counterpart of <see cref="ReqnrollConfigChangedHandler"/>:
/// both follow the same pattern of finding affected feature files, re-parsing, and notifying
/// downstream consumers via <see cref="GherkinDocumentParsedNotification"/>.
/// The key difference is that this handler is triggered after the out-of-process connector
/// has completed a successful discovery run and the per-project
/// <see cref="Discovery.ConnectorBindingRegistryProvider.Current"/> has been atomically swapped,
/// so the re-parse will immediately see the new bindings.
/// </remarks>
public class BindingRegistryChangedHandler : INotificationHandler<BindingRegistryChangedNotification>
{
    private readonly IDocumentBufferService       _documentBufferService;
    private readonly IGherkinDocumentTaggerService _taggerService;
    private readonly IMediator                    _mediator;
    private readonly IDeveroomLogger               _logger;

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

        var affectedBuffers = _documentBufferService.All
            .Where(b => IsUnderProjectFolder(b.Uri, projectFolder))
            .ToList();

        if (affectedBuffers.Count == 0)
        {
            _logger.LogVerbose(
                $"BindingRegistryChanged for '{notification.Project.ProjectName}' " +
                $"— no open feature files to reparse.");
            return;
        }

        _logger.LogInfo(
            $"BindingRegistryChanged for '{notification.Project.ProjectName}' " +
            $"— reparsing {affectedBuffers.Count} feature file(s).");

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
        // ParseAsync stores updated tags and invalidates the semantic token cache internally.
        var tags = await _taggerService.ParseAsync(uri, version).ConfigureAwait(false);
        await _mediator.Publish(
            new GherkinDocumentParsedNotification(uri, version ?? 0, tags),
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
