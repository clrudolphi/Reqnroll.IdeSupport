using Reqnroll.IdeSupport.Common;
using Reqnroll.IdeSupport.Common.Diagnostics;
using System;

namespace Reqnroll.IdeSupport.LSP.Server.Workspace;

/// <summary>
/// Server-level singleton implementation of <see cref="IIdeScope"/> for the LSP server process.
/// Provides infrastructure services (logger, file system, monitoring) to project scopes and
/// configuration providers without any VSSDK dependency.
/// </summary>
public sealed class LspIdeScope : IIdeScope
{
    public LspIdeScope(IDeveroomLogger logger)
    {
        Logger = logger;
        FileSystem = new FileSystemForIDE();
        MonitoringService = NullMonitoringService.Instance;
        Actions = new LspIdeActions(logger);
    }

    public bool IsSolutionLoaded => true;
    public IDeveroomLogger Logger { get; }
    public IMonitoringService MonitoringService { get; }
    public IIdeActions Actions { get; }
    public IFileSystemForIDE FileSystem { get; }

    // ── Inner type ────────────────────────────────────────────────────────────

    private sealed class LspIdeActions : IIdeActions
    {
        private readonly IDeveroomLogger _logger;

        public LspIdeActions(IDeveroomLogger logger) => _logger = logger;

        public void ShowError(string description, Exception exception)
            => _logger.LogError($"{description}: {exception}");

        public void ShowProblem(string message)
            => _logger.LogWarning(message);
    }
}
