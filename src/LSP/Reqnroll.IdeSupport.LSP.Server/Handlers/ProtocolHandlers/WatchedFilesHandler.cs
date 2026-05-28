using MediatR;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Workspace;
using Reqnroll.IdeSupport.Common.Diagnostics;
using Reqnroll.IdeSupport.Common.ProjectSystem;
using Reqnroll.IdeSupport.Common.ProjectSystem.Configuration;
using Reqnroll.IdeSupport.LSP.Server.Notifications;
using Reqnroll.IdeSupport.LSP.Server.Workspace;
using System.Threading;
using System.Threading.Tasks;

namespace Reqnroll.IdeSupport.LSP.Server.Handlers.ProtocolHandlers;

/// <summary>
/// Handles <c>workspace/didChangeWatchedFiles</c> notifications for <c>reqnroll.json</c> files.
/// When a <c>reqnroll.json</c> is created, changed, or deleted the owning workspace scope's
/// configuration is reloaded and a <see cref="ReqnrollConfigChangedNotification"/> is published
/// so that all open feature files in that workspace are re-parsed.
/// </summary>
public class WatchedFilesHandler : IDidChangeWatchedFilesHandler
{
    private readonly ILspWorkspaceScopeManager _scopeManager;
    private readonly IMediator _mediator;
    private readonly IDeveroomLogger _logger;

    public WatchedFilesHandler(
        ILspWorkspaceScopeManager scopeManager,
        IMediator mediator,
        IDeveroomLogger logger)
    {
        _scopeManager = scopeManager;
        _mediator = mediator;
        _logger = logger;
    }

    public DidChangeWatchedFilesRegistrationOptions GetRegistrationOptions(
        DidChangeWatchedFilesCapability capability,
        ClientCapabilities clientCapabilities)
        => new()
        {
            Watchers = new[]
            {
                new OmniSharp.Extensions.LanguageServer.Protocol.Models.FileSystemWatcher
                {
                    GlobPattern = "**/reqnroll.json",
                    Kind = WatchKind.Create | WatchKind.Change | WatchKind.Delete
                }
            }
        };

    public async Task<Unit> Handle(DidChangeWatchedFilesParams request, CancellationToken cancellationToken)
    {
        foreach (var fileEvent in request.Changes)
        {
            var uri = fileEvent.Uri;
            var changeType = fileEvent.Type;

            var scope = _scopeManager.GetScopeForUri(uri) as LspProjectScope;
            if (scope == null)
            {
                _logger.LogVerbose($"reqnroll.json event ({changeType}) for {uri} — no matching workspace scope, skipping.");
                continue;
            }

            _logger.LogInfo($"reqnroll.json {changeType}: {uri} — reloading configuration for workspace '{scope.ProjectFolder}'");

            var provider = scope.GetDeveroomConfigurationProvider() as ProjectScopeDeveroomConfigurationProvider;
            provider?.Reload();

            await _mediator.Publish(
                new ReqnrollConfigChangedNotification(scope.ProjectFolder),
                cancellationToken).ConfigureAwait(false);
        }

        return Unit.Value;
    }
}
