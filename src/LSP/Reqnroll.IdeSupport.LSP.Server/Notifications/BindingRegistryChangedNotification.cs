using MediatR;
using Reqnroll.IdeSupport.LSP.Server.Workspace;

namespace Reqnroll.IdeSupport.LSP.Server.Notifications;

/// <summary>
/// Published when a project's <see cref="Reqnroll.IdeSupport.LSP.Core.Discovery.ProjectBindingRegistry"/>
/// is replaced after a successful connector discovery run (e.g. triggered by a build or a
/// <c>reqnroll.json</c> change).
/// </summary>
/// <remarks>
/// Consumers should re-parse every open feature file that belongs to
/// <see cref="Project"/> so that step-definition tags are evaluated against the
/// new registry, then publish <see cref="MatchCacheChangedNotification"/> for each
/// to trigger a semantic-token refresh.
/// </remarks>
public record BindingRegistryChangedNotification(
    LspReqnrollProject Project) : INotification;
