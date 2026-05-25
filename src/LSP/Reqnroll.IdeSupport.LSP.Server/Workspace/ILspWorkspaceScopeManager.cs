using OmniSharp.Extensions.LanguageServer.Protocol;
using Reqnroll.IdeSupport.Common.ProjectSystem;
using Reqnroll.IdeSupport.Common.ProjectSystem.Configuration;

namespace Reqnroll.IdeSupport.LSP.Server.Workspace;

/// <summary>
/// Manages per-workspace <see cref="LspProjectScope"/> instances.
/// Workspace roots are opened/closed in response to LSP workspace folder events
/// and the <c>initialize</c> handshake.
/// </summary>
public interface ILspWorkspaceScopeManager
{
    /// <summary>Registers a new workspace root, creating a <see cref="LspProjectScope"/> for it.</summary>
    void OpenWorkspace(string rootPath);

    /// <summary>Disposes and removes the scope associated with <paramref name="rootPath"/>.</summary>
    void CloseWorkspace(string rootPath);

    /// <summary>
    /// Returns the <see cref="IProjectScope"/> whose root is the closest ancestor of
    /// the local file path represented by <paramref name="uri"/>, or <c>null</c> if
    /// no registered workspace covers the URI.
    /// </summary>
    IProjectScope? GetScopeForUri(DocumentUri uri);

    /// <summary>
    /// Returns the <see cref="IDeveroomConfigurationProvider"/> for the workspace that
    /// covers <paramref name="uri"/>, falling back to a default provider when no scope matches.
    /// </summary>
    IDeveroomConfigurationProvider GetConfigurationProviderForUri(DocumentUri uri);
}
