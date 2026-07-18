using Reqnroll.IdeSupport.Common.Logging;
using Reqnroll.IdeSupport.Common.ProjectSystem;
using Reqnroll.IdeSupport.LSP.Connector.Models;
using Reqnroll.IdeSupport.LSP.Core.Bindings;

namespace Reqnroll.IdeSupport.LSP.Server.Discovery;

/// <summary>
/// Orchestrates one binding-discovery run against a project scope.
/// Invokes the appropriate <see cref="OutProcReqnrollConnector"/> (generic or custom),
/// converts the <see cref="DiscoveryResult"/> into a <see cref="ProjectBindingRegistry"/>
/// via <see cref="BindingImporter"/>, and guards with an assembly-hash check to suppress
/// no-op re-runs.
/// </summary>
/// <remarks>
/// This service is stateless and synchronous; callers run it on a background thread
/// (typically via <see cref="ConnectorBindingRegistryProvider"/>).  Connector construction
/// is delegated to an <see cref="IOutProcConnectorFactory"/> so the selection of generic vs
/// custom connector lives in one place and this orchestrator can be tested with a fake.
/// </remarks>
public sealed class ConnectorDiscoveryService : IConnectorDiscoveryService
{
    private readonly IIdeSupportLogger _logger;
    private readonly IOutProcConnectorFactory _connectorFactory;

    /// <summary>Initializes a new instance of the <see cref="ConnectorDiscoveryService"/> class.</summary>
    public ConnectorDiscoveryService(IIdeSupportLogger logger, IOutProcConnectorFactory connectorFactory)
    {
        _logger = logger;
        _connectorFactory = connectorFactory;
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Runs discovery for <paramref name="scope"/>.
    /// </summary>
    /// <returns>
    /// A new <see cref="ProjectBindingRegistry"/> and its content hash when discovery
    /// succeeds.  Returns (<paramref name="lastGood"/>, <paramref name="lastHash"/>) unchanged
    /// when the assembly is missing, unchanged, or the connector fails.
    /// </returns>
    public (ProjectBindingRegistry Registry, string Hash) RunDiscovery(
        IProjectScope scope,
        ProjectBindingRegistry lastGood,
        string lastHash,
        CancellationToken ct)
    {
        var assemblyPath = scope.OutputAssemblyPath;

        if (string.IsNullOrEmpty(assemblyPath))
        {
            _logger.LogVerbose($"[{scope.ProjectName}] OutputAssemblyPath not set; skipping discovery.");
            return (lastGood, lastHash);
        }

        if (!File.Exists(assemblyPath))
        {
            _logger.LogInfo($"[{scope.ProjectName}] Output assembly not found (project not yet built?): {assemblyPath}");
            return (lastGood, lastHash);
        }

        var currentHash = ComputeHash(assemblyPath);
        if (currentHash == lastHash)
        {
            _logger.LogVerbose($"[{scope.ProjectName}] Assembly unchanged (hash match); skipping discovery.");
            return (lastGood, lastHash);
        }

        ct.ThrowIfCancellationRequested();

        var connector = _connectorFactory.Create(scope);
        var configFilePath = FindConfigFilePath(scope);

        _logger.LogInfo($"[{scope.ProjectName}] Starting binding discovery: {Path.GetFileName(assemblyPath)}");

        DiscoveryResult result;
        var sw = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            result = connector.RunDiscovery(assemblyPath, configFilePath);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning($"[{scope.ProjectName}] Connector invocation failed after {sw.ElapsedMilliseconds}ms: {ex.Message}");
            return (lastGood, lastHash);
        }
        sw.Stop();

        ct.ThrowIfCancellationRequested();

        LogWarningsAndErrors(scope, result);

        if (result.IsFailed)
        {
            _logger.LogWarning($"[{scope.ProjectName}] Discovery failed after {sw.ElapsedMilliseconds}ms: {result.ErrorMessage}");
            return (lastGood, lastHash);
        }

        var registry = BuildRegistry(scope, result);
        _logger.LogInfo(
            $"[{scope.ProjectName}] Discovery complete in {sw.ElapsedMilliseconds}ms: " +
            $"{registry.StepDefinitions.Length} step definition(s), {registry.Hooks.Length} hook(s).");
        return (registry, currentHash);
    }

    // ── Registry building ─────────────────────────────────────────────────────

    private ProjectBindingRegistry BuildRegistry(IProjectScope scope, DiscoveryResult result)
    {
        var importer = new BindingImporter(result.SourceFiles, result.TypeNames, _logger);

        var stepDefinitions = (result.StepDefinitions ?? [])
            .Select(sd => {
                // For connector-discovered bindings, backfill the attribute source line
                // from the source file using Roslyn syntax parsing. This enables exact
                // AST-based matching in FindBindingAtLocation instead of the heuristic
                // line window that was the only option when AttributeSourceLine was null.
                var sourceFile = ResolveSourceFile(sd, result);
                var attrLine = sourceFile != null
                    ? BindingImporter.TryGetAttributeSourceLine(sourceFile, sd.Method)
                    : null;
                return importer.ImportStepDefinition(sd, attrLine);
            })
            .Where(sd => sd is not null)
            .ToList();

        var hooks = (result.Hooks ?? [])
            .Select(h => importer.ImportHook(h))
            .Where(h => h is not null)
            .ToList();

        // Use a stable hash of the output path as the project hash so the registry
        // can participate in the version-monotonicity guard in ProjectBindingRegistry.
        var projectHash = scope.OutputAssemblyPath.GetHashCode();
        return new ProjectBindingRegistry(stepDefinitions!, hooks!, projectHash);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns a content-change key combining the assembly path with its last-write time.
    /// Two calls with the same result mean no rebuild has happened.
    /// </summary>
    private static string ComputeHash(string assemblyPath)
    {
        try
        {
            var lastWrite = File.GetLastWriteTimeUtc(assemblyPath);
            return $"{assemblyPath}@{lastWrite.Ticks}";
        }
        catch
        {
            return assemblyPath;
        }
    }

    /// Resolves the absolute source file path from a step definition's source location reference
    /// (which uses the connector's "#index" format to reference a shared source-file table).
    private static string? ResolveSourceFile(StepDefinition sd, DiscoveryResult result)
    {
        var sourceLocationRaw = sd.SourceLocation;
        if (string.IsNullOrWhiteSpace(sourceLocationRaw)) return null;

        var sourceRef = sourceLocationRaw.Split('|')[0];
        if (sourceRef.StartsWith("#") && result.SourceFiles != null &&
            result.SourceFiles.TryGetValue(sourceRef.Substring(1), out var resolvedPath))
            return resolvedPath;

        // Already an absolute path — use it directly if the file exists.
        return File.Exists(sourceRef) ? sourceRef : null;
    }

    private static string FindConfigFilePath(IProjectScope scope)
    {
        // Search standard Reqnroll/SpecFlow config file names relative to the project folder.
        var candidates = new[]
        {
            Path.Combine(scope.ProjectFolder, "reqnroll.json"),
            //Path.Combine(scope.ProjectFolder, "specflow.json"),
            //Path.Combine(scope.ProjectFolder, "app.config")
        };
        return Array.Find(candidates, File.Exists) ?? string.Empty;
    }

    private void LogWarningsAndErrors(IProjectScope scope, DiscoveryResult result)
    {
        if (result.Warnings is not null)
            foreach (var w in result.Warnings)
                _logger.LogWarning($"[{scope.ProjectName}] {w}");

        if (result.GenericBindingErrors is not null)
            foreach (var e in result.GenericBindingErrors)
                _logger.LogWarning($"[{scope.ProjectName}] Binding error: {e}");
    }
}
