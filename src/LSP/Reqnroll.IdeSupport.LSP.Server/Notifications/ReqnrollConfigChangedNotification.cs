using MediatR;

namespace Reqnroll.IdeSupport.LSP.Server.Notifications;

/// <summary>
/// Published internally when a <c>reqnroll.json</c> file is created, changed, or deleted
/// in a workspace root. Consumers should re-parse all feature files in the affected workspace.
/// </summary>
public record ReqnrollConfigChangedNotification(string WorkspaceRootPath) : INotification;
