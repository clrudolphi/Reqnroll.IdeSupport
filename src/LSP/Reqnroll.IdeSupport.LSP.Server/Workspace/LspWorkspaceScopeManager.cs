using OmniSharp.Extensions.LanguageServer.Protocol;
using Reqnroll.IdeSupport.Common;
using Reqnroll.IdeSupport.Common.Diagnostics;
using Reqnroll.IdeSupport.Common.ProjectSystem.Configuration;
using Reqnroll.IdeSupport.LSP.Server.Discovery;
using Reqnroll.IdeSupport.LSP.Server.Protocol;

namespace Reqnroll.IdeSupport.LSP.Server.Workspace;

/// <summary>
/// Thread-safe implementation of <see cref="ILspWorkspaceScopeManager"/>.
/// </summary>
public sealed class LspWorkspaceScopeManager : ILspWorkspaceScopeManager, IDisposable
{
    private readonly IIdeScope _ideScope;
    private readonly IDeveroomLogger _logger;

    private readonly ConcurrentDictionary<string, LspProjectScope> _scopes
        = new(StringComparer.OrdinalIgnoreCase);

    public LspWorkspaceScopeManager(IIdeScope ideScope, IDeveroomLogger logger)
    {
        _ideScope = ideScope;
        _logger   = logger;
    }

    // ── Folder lifecycle ──────────────────────────────────────────────────────

    public event Action<LspProjectScope>? ScopeOpened;
    public event Action<LspProjectScope>? ScopeClosed;

    public void OpenWorkspace(string rootPath)
    {
        var key = Normalise(rootPath);
        LspProjectScope? added = null;
        _scopes.GetOrAdd(key, k =>
        {
            _logger.LogInfo($"Opening workspace scope: {k}");
            added = new LspProjectScope(k, _ideScope);
            return added;
        });
        if (added is not null)
            ScopeOpened?.Invoke(added);
    }

    public void CloseWorkspace(string rootPath)
    {
        var key = Normalise(rootPath);
        if (!_scopes.TryRemove(key, out var scope))
            return;

        _logger.LogInfo($"Closing workspace scope: {key}");

        // Raise ProjectRemoved for every project still inside the scope.
        foreach (var project in scope.Projects)
        {
            ProjectRemoved?.Invoke(project);
        }

        ScopeClosed?.Invoke(scope);
        scope.Dispose();
    }

    // ── Project lifecycle ─────────────────────────────────────────────────────

    public event Action<LspReqnrollProject>? ProjectDiscovered;
    public event Action<LspReqnrollProject>? ProjectRemoved;

    public Task HandleProjectLoadedAsync(
        ReqnrollProjectLoadedParams parameters,
        CancellationToken cancellationToken)
    {
        // Ensure the workspace folder exists (create it if the IDE sends the project
        // notification before the LSP initialize workspace-folders arrive).
        var folderKey = Normalise(parameters.WorkspaceFolder);
        var scope = _scopes.GetOrAdd(folderKey, k =>
        {
            _logger.LogInfo($"Auto-creating workspace scope for project notification: {k}");
            var newScope = new LspProjectScope(k, _ideScope);
            ScopeOpened?.Invoke(newScope);
            return newScope;
        });

        var (project, isNew, discoveryInputChanged) = scope.AddOrUpdateProject(parameters);

        if (isNew)
        {
            _logger.LogInfo(
                $"Project discovered: {project.ProjectName} " +
                $"[{project.TargetFrameworkMoniker}] → {project.OutputAssemblyPath}");
            // ProjectDiscovered subscribers (BindingRegistryProviderRouter) create the
            // per-project provider and trigger the initial discovery, so no explicit
            // refresh is needed here for a brand-new project.
            ProjectDiscovered?.Invoke(project);
        }
        else
        {
            _logger.LogInfo(
                $"Project updated: {project.ProjectName} " +
                $"[{project.TargetFrameworkMoniker}] → {project.OutputAssemblyPath}");

            // An existing project whose output assembly path or target framework changed
            // (e.g. a rebuild, or a Debug→Release switch that moves the output path) must
            // re-run binding discovery.  The output-assembly file watcher does not reliably
            // cover the path-change case: GetProjectByOutputPath matches on the *old* path
            // until this update lands, so the watcher event for the new DLL can be dropped.
            if (discoveryInputChanged)
                TriggerBindingDiscovery(project);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Triggers a debounced binding re-discovery on the per-project
    /// <see cref="ConnectorBindingRegistryProvider"/> stored in the project's property bag,
    /// if one has been registered by <see cref="BindingRegistryProviderRouter"/>.
    /// </summary>
    private void TriggerBindingDiscovery(LspReqnrollProject project)
    {
        if (project.Properties.TryGetValue(
                typeof(ConnectorBindingRegistryProvider), out var obj)
            && obj is ConnectorBindingRegistryProvider provider)
        {
            _logger.LogVerbose(
                $"[{project.ProjectName}] Discovery inputs changed; triggering re-discovery.");
            provider.TriggerRefresh();
        }
        else
        {
            _logger.LogVerbose(
                $"[{project.ProjectName}] Discovery inputs changed but no binding provider " +
                $"registered yet; skipping refresh.");
        }
    }

    public Task HandleProjectUnloadedAsync(
        ReqnrollProjectUnloadedParams parameters,
        CancellationToken cancellationToken)
    {
        foreach (var scope in _scopes.Values)
        {
            var removed = scope.RemoveProject(parameters.ProjectFile);
            if (removed is null)
                continue;

            _logger.LogInfo($"Project removed: {removed.ProjectName}");
            ProjectRemoved?.Invoke(removed);
            removed.Dispose();
            return Task.CompletedTask;
        }

        _logger.LogVerbose(
            $"HandleProjectUnloadedAsync: no project found for {parameters.ProjectFile}");
        return Task.CompletedTask;
    }

    // ── Lookup ────────────────────────────────────────────────────────────────

    public LspProjectScope? GetScopeForUri(DocumentUri uri)
    {
        var filePath = uri.GetFileSystemPath();
        if (string.IsNullOrEmpty(filePath))
            return null;

        return _scopes.Values
            .Where(s => filePath.StartsWith(s.RootFolder, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(s => s.RootFolder.Length)
            .FirstOrDefault();
    }

    public LspReqnrollProject? GetProjectForUri(DocumentUri uri)
    {
        var filePath = uri.GetFileSystemPath();
        if (string.IsNullOrEmpty(filePath))
            return null;

        return _scopes.Values
            .SelectMany(s => s.Projects)
            .Where(p => filePath.StartsWith(p.ProjectFolder, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(p => p.ProjectFolder.Length)
            .FirstOrDefault();
    }

    public LspReqnrollProject? GetProjectByOutputPath(string assemblyPath)
    {
        if (string.IsNullOrEmpty(assemblyPath))
            return null;

        return _scopes.Values
            .SelectMany(s => s.Projects)
            .FirstOrDefault(p => string.Equals(
                p.OutputAssemblyPath, assemblyPath,
                StringComparison.OrdinalIgnoreCase));
    }

    public IDeveroomConfigurationProvider GetConfigurationProviderForUri(DocumentUri uri)
    {
        var project = GetProjectForUri(uri);
        if (project is not null)
            return project.GetDeveroomConfigurationProvider();

        // Fallback: default configuration when no project covers the URI.
        return new ProjectSystemDeveroomConfigurationProvider(_ideScope);
    }

    // ── IDisposable ───────────────────────────────────────────────────────────

    public void Dispose()
    {
        foreach (var key in _scopes.Keys.ToArray())
            CloseWorkspace(key);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string Normalise(string path)
        => Path.GetFullPath(path).TrimEnd(
            Path.DirectorySeparatorChar,
            Path.AltDirectorySeparatorChar);
}
