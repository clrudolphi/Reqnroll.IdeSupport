using System.Diagnostics;
using System.IO;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.Extensibility.LanguageServer;
using Microsoft.VisualStudio.RpcContracts.LanguageServerProvider;
using Microsoft.VisualStudio.Shell;
using Nerdbank.Streams;
using Reqnroll.IdeSupport.VisualStudio.Extension.Classification;
using Reqnroll.IdeSupport.VisualStudio.Extension.LspInterception;
using Reqnroll.IdeSupport.VisualStudio.Extension.LspNotifications;
#pragma warning disable VSEXTPREVIEW_LSP

namespace Reqnroll.IdeSupport.VisualStudio.Extension;

[VisualStudioContribution]
internal class ReqnrollLanguageClient : LanguageServerProvider
{
    private readonly TraceSource _traceSource;
    private Process? _serverProcess;
    private LspInspectorLogger? _inspectorLogger;
    private LspInterceptingPipe? _interceptingPipe;
    private ChildProcessJob? _childJob;
    private VsProjectEventMonitor? _projectMonitor;

    public ReqnrollLanguageClient(
        ExtensionCore container,
        VisualStudioExtensibility extensibilityObject,
        TraceSource traceSource)
        : base(container, extensibilityObject)
    {
        _traceSource = traceSource;
        _traceSource.TraceInformation("ReqnrollLanguageClient: Instance created.");
    }

    /// <inheritdoc />
    public override LanguageServerProviderConfiguration LanguageServerProviderConfiguration =>
        new("Reqnroll Language Client",
            new[]
            {
                DocumentFilter.FromDocumentType(GherkinDocumentType.GherkinDocument),
            });

    /// <inheritdoc />
    public override async Task<IDuplexPipe?> CreateServerConnectionAsync(CancellationToken cancellationToken)
    {
        var serverExe = Path.Combine(
            Path.GetDirectoryName(typeof(ReqnrollLanguageClient).Assembly.Location)!,
            "LSPServer",
            "Reqnroll.IdeSupport.LSP.Server.exe");

        _traceSource.TraceInformation("ReqnrollLanguageClient: CreateServerConnectionAsync called. Server exe path: {0}", serverExe);

        if (!File.Exists(serverExe))
        {
            _traceSource.TraceEvent(TraceEventType.Error, 0,
                "ReqnrollLanguageClient: Server executable not found at '{0}'. Disabling.", serverExe);
            Enabled = false;
            return null;
        }

        try
        {
            var psi = new ProcessStartInfo(serverExe)
            {
                UseShellExecute        = false,
                RedirectStandardInput  = true,
                RedirectStandardOutput = true,
                RedirectStandardError  = true,
                CreateNoWindow         = true,
                // Tell the LSP server which IDE is connecting so it selects the correct
                // semantic token profile (legend + DeveroomTag→token type mapping).
                Arguments              = "--ide visualstudio",
            };

            _serverProcess = Process.Start(psi)
                ?? throw new InvalidOperationException("Process.Start returned null.");

            // Assign to a kill-on-close Job Object so the server is terminated by the OS
            // when this VS process exits, even if Dispose is never called.
            try
            {
                _childJob = new ChildProcessJob();
                _childJob.AddProcess(_serverProcess);
            }
            catch (Exception ex)
            {
                _traceSource.TraceEvent(TraceEventType.Warning, 0,
                    "ReqnrollLanguageClient: Could not assign server to Job Object: {0}", ex.Message);
            }

            _serverProcess.ErrorDataReceived += (_, e) =>
            {
                if (e.Data is not null)
                    _traceSource.TraceEvent(TraceEventType.Warning, 0, "LSPServer stderr: {0}", e.Data);
            };
            _serverProcess.BeginErrorReadLine();

            _traceSource.TraceInformation("ReqnrollLanguageClient: Server process started (PID {0}).", _serverProcess.Id);

            IDuplexPipe rawPipe = new DuplexPipe(
                _serverProcess.StandardOutput.BaseStream.UsePipeReader(),
                _serverProcess.StandardInput.BaseStream.UsePipeWriter());

            // Build the LSP Inspector log file path, unique per session.
            var logDir     = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Reqnroll");
            var logFile    = Path.Combine(logDir, $"lsp-inspector-{DateTime.Now:yyyyMMdd-HHmmss}.log");
            _inspectorLogger = new LspInspectorLogger(logFile, _traceSource);

            // Observes semanticTokens traffic (both directions) and caches the decoded tokens so the
            // editor classifier can colour .feature files with Reqnroll's custom classifications,
            // bypassing VS's fixed built-in token-type→classification table. One instance is shared
            // by both pipelines so it sees requests (VS→Server) and their responses (Server→VS).
            var semanticTokensInterceptor = new SemanticTokensClassificationInterceptor(
                SemanticTokenClassificationStore.Instance, _traceSource);

            // Send pipeline:   VS → [logger, semanticTokens] → Server
            // Receive pipeline: Server → [logger, semanticTokens] → VS
            // Add consuming interceptors to the receive list as features are added.
            var sendInterceptors    = new ILspMessageInterceptor[] { _inspectorLogger, semanticTokensInterceptor };
            var receiveInterceptors = new ILspMessageInterceptor[] { _inspectorLogger, semanticTokensInterceptor };

            _interceptingPipe = new LspInterceptingPipe(rawPipe, sendInterceptors, receiveInterceptors, _traceSource);
            // Pass CancellationToken.None: the pumps must live for the entire connection
            // lifetime, not just for the duration of this async creation call.  The pipe's
            // own _cts (cancelled in Dispose) provides the shutdown signal.
            await _interceptingPipe.StartAsync(CancellationToken.None).ConfigureAwait(false);

            return _interceptingPipe.VsFacingPipe;
        }
        catch (Exception ex)
        {
            _traceSource.TraceEvent(TraceEventType.Error, 0,
                "ReqnrollLanguageClient: Failed to start server: {0}", ex);
            Enabled = false;
            return null;
        }
    }

    /// <inheritdoc />
    public override async Task OnServerInitializationResultAsync(
        ServerInitializationResult serverInitializationResult,
        LanguageServerInitializationFailureInfo? initializationFailureInfo,
        CancellationToken cancellationToken)
    {
        if (serverInitializationResult == ServerInitializationResult.Failed)
        {
            _traceSource.TraceEvent(TraceEventType.Error, 0,
                "ReqnrollLanguageClient: Server initialization failed. Info: {0}",
                initializationFailureInfo?.StatusMessage ?? initializationFailureInfo?.Exception?.Message ?? "(none)");
            Enabled = false;
            return;
        }

        _traceSource.TraceInformation(
            "ReqnrollLanguageClient: Server initialized successfully ({0}).",
            serverInitializationResult);

        // Start monitoring VS project events and flush the current solution state.
        if (_interceptingPipe is not null)
        {
            try
            {
                var serviceProvider = ServiceProvider.GlobalProvider;
                _projectMonitor = new VsProjectEventMonitor(
                    _interceptingPipe, _traceSource, serviceProvider);

                await _projectMonitor
                    .SendInitialProjectsAsync(cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _traceSource.TraceEvent(TraceEventType.Warning, 0,
                    "ReqnrollLanguageClient: Could not start project monitor: {0}", ex.Message);
            }
        }
    }

    /// <inheritdoc />
    protected override void Dispose(bool isDisposing)
    {
        if (isDisposing)
        {
            _projectMonitor?.Dispose();
            _projectMonitor = null;

            _interceptingPipe?.Dispose();
            _interceptingPipe = null;

            _inspectorLogger?.Dispose();
            _inspectorLogger = null;

            try { _serverProcess?.Kill(); } catch { /* best-effort */ }
            _serverProcess?.Dispose();
            _serverProcess = null;

            _childJob?.Dispose();
            _childJob = null;
        }

        base.Dispose(isDisposing);
    }
}
