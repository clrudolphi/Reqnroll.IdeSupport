using Reqnroll.IdeSupport.Common;
using Reqnroll.IdeSupport.Common.Logging;
using Reqnroll.IdeSupport.Common.Telemetry;
using Reqnroll.IdeSupport.LSP.Server.Telemetry;

namespace Reqnroll.IdeSupport.LSP.Server.Workspace;

/// <summary>
/// Server-level singleton implementation of <see cref="IIdeScope"/> for the LSP server process.
/// Provides infrastructure services (logger, file system, monitoring) to project scopes and
/// configuration providers without any VSSDK dependency.
/// </summary>
public sealed class LspIdeScope : IIdeScope
{
    /// <summary>Initializes a new instance of the <see cref="LspIdeScope"/> class.</summary>
    public LspIdeScope(IIdeSupportLogger logger)
    {
        Logger = logger;
        FileSystem = new FileSystemForIDE();
        TelemetryService = NullTelemetryService.Instance;
        Actions = new LspIdeActions(logger);
    }

    /// <summary>Gets or sets the is solution loaded.</summary>
    public bool IsSolutionLoaded => true;
    /// <summary>Gets or sets the logger.</summary>
    public IIdeSupportLogger Logger { get; }
    /// <summary>Gets or sets the telemetry service.</summary>
    public ITelemetryService TelemetryService { get; }
    /// <summary>Gets or sets the actions.</summary>
    public IIdeActions Actions { get; }
    /// <summary>Gets or sets the file system.</summary>
    public IFileSystemForIDE FileSystem { get; }

    // ── Inner type ────────────────────────────────────────────────────────────

    private sealed class LspIdeActions : IIdeActions
    {
        private readonly IIdeSupportLogger _logger;

        public LspIdeActions(IIdeSupportLogger logger) => _logger = logger;

        public void ShowError(string description, Exception exception)
            => _logger.LogError($"{description}: {exception}");

        public void ShowProblem(string message)
            => _logger.LogWarning(message);
    }
}
