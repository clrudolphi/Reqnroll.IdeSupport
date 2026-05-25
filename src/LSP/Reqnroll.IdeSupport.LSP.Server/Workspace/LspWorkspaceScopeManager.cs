using OmniSharp.Extensions.LanguageServer.Protocol;
using Reqnroll.IdeSupport.Common;
using Reqnroll.IdeSupport.Common.Diagnostics;
using Reqnroll.IdeSupport.Common.ProjectSystem;
using Reqnroll.IdeSupport.Common.ProjectSystem.Configuration;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;

namespace Reqnroll.IdeSupport.LSP.Server.Workspace;

/// <summary>
/// Thread-safe registry of <see cref="LspProjectScope"/> instances, one per open workspace folder.
/// </summary>
public sealed class LspWorkspaceScopeManager : ILspWorkspaceScopeManager, IDisposable
{
    private readonly IIdeScope _ideScope;
    private readonly IDeveroomLogger _logger;
    private readonly ConcurrentDictionary<string, LspProjectScope> _scopes = new(StringComparer.OrdinalIgnoreCase);

    public LspWorkspaceScopeManager(IIdeScope ideScope, IDeveroomLogger logger)
    {
        _ideScope = ideScope;
        _logger = logger;
    }

    // ── ILspWorkspaceScopeManager ─────────────────────────────────────────────

    public void OpenWorkspace(string rootPath)
    {
        var normalized = Normalize(rootPath);
        _scopes.GetOrAdd(normalized, key =>
        {
            _logger.LogInfo($"Opening workspace scope: {key}");
            return new LspProjectScope(key, _ideScope);
        });
    }

    public void CloseWorkspace(string rootPath)
    {
        var normalized = Normalize(rootPath);
        if (_scopes.TryRemove(normalized, out var scope))
        {
            _logger.LogInfo($"Closing workspace scope: {normalized}");
            scope.Dispose();
        }
    }

    public IProjectScope? GetScopeForUri(DocumentUri uri)
    {
        var filePath = uri.GetFileSystemPath();
        if (string.IsNullOrEmpty(filePath))
            return null;

        // Find the registered root that is the longest prefix match for the file path.
        return _scopes.Values
            .Where(s => filePath.StartsWith(s.ProjectFolder, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(s => s.ProjectFolder.Length)
            .FirstOrDefault();
    }

    public IDeveroomConfigurationProvider GetConfigurationProviderForUri(DocumentUri uri)
    {
        var scope = GetScopeForUri(uri);
        if (scope != null)
            return scope.GetDeveroomConfigurationProvider();

        // Fallback: default configuration when no workspace covers the URI.
        return new ProjectSystemDeveroomConfigurationProvider(_ideScope);
    }

    // ── IDisposable ───────────────────────────────────────────────────────────

    public void Dispose()
    {
        foreach (var key in _scopes.Keys.ToArray())
            CloseWorkspace(key);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string Normalize(string path)
        => Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
}
