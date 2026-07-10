using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Reqnroll.IdeSupport.Common;
using Reqnroll.IdeSupport.Common.Analytics;
using Reqnroll.IdeSupport.Common.Logging;
using Reqnroll.IdeSupport.VisualStudio.Wizards.VsIntegration;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using Task = System.Threading.Tasks.Task;

namespace Reqnroll.IdeSupport.VisualStudio.Extension;

// Q23: ProvideAutoLoad ensures the package loads when a solution exists, even when no
// .feature file is the foreground editor. Without this, the LSP server never starts on
// session restore if the foreground tab is a .cs file (scenario A).
[ProvideAutoLoad(
    UIContextGuids80.SolutionExists,
    PackageAutoLoadFlags.BackgroundLoad)]
[PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
// Put the extension install folder on VS's assembly binding path. Without this, VS can only
// load our assemblies by *path* (which MEF does via catalog.json) — but identity-based loads
// fail: the VsPackage itself (Assembly.Load of this assembly's strong name from the pkgdef)
// and the item/project template wizards (loaded by the template engine via the strong name in
// each .vstemplate's <WizardExtension>) both resolve by identity and otherwise get
// FileNotFound. The Microsoft.VisualStudio.Assembly manifest assets are not honored as
// codebases in the VSSDK+VisualStudio.Extensibility hybrid, so a BindingPath is required.
[ProvideBindingPath]
[Guid(PackageGuidString)]
public sealed class ReqnrollPluginPackage : AsyncPackage
{
    public const string PackageGuidString = "8d5fe503-e038-4079-9e45-697e0dcb3758";

    private IIdeSupportLogger _logger = new IdeSupportNullLogger();
    private IAnalyticsTransmitter? _analyticsTransmitter;

    protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
    {
        await base.InitializeAsync(cancellationToken, progress);

        // Resolve the shared MEF-exported IIdeSupportLogger (issue #84) rather than a private
        // ad-hoc SynchronousFileLogger + a second, unlistened-to TraceSource. Resolved early
        // (alongside IAnalyticsTransmitter below) so it's available for the full package lifecycle;
        // falls back to a no-op logger only if the component model isn't ready yet.
        {
            var sp = await GetServiceAsync(typeof(SComponentModel)) as IServiceProvider;
            if (sp != null)
            {
                _logger = VsUtils.ResolveMefDependency<IIdeSupportLogger>(sp) ?? new IdeSupportNullLogger();
                _analyticsTransmitter = VsUtils.ResolveMefDependency<IAnalyticsTransmitter>(sp);
            }
        }

        _logger.LogInfo("ReqnrollPluginPackage: InitializeAsync started.");
        _logger.LogInfo("Waiting for solution load...");

        await WaitForSolutionLoadAsync(cancellationToken);

        // NOTE: We intentionally do NOT realize .feature stub frames here. Doing so at
        // solution load races with VS's own restore of feature tabs and spawns a second
        // LSP server process, leaving the editor bound to an unmatched server (no step
        // parameter coloring / no CodeLens usage counts). The LanguageServerProvider
        // activates the normal way (VS realizes a feature tab, or the user opens a
        // feature file), and ReqnrollLanguageClient.OnServerInitializationResultAsync
        // flushes any remaining stubs at the safe post-server-init point.
        // TODO(Q23): reinstate a non-racing/idempotent way to start the LSP when the
        // foreground tab is a .cs file and no feature file is open.

        _logger.LogInfo("Solution loaded.");

        // Show the Welcome (first install) or Upgrade (version change) dialog
        // if appropriate, after a short delay so VS can finish initializing.
        await RunWelcomeServiceAsync(cancellationToken);

        _logger.LogInfo("Package initialisation complete.");
    }

    private async Task RunWelcomeServiceAsync(CancellationToken cancellationToken)
    {
        _logger.LogInfo("RunWelcomeServiceAsync: starting.");
        try
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            var sp = ServiceProvider.GlobalProvider;

            // Resolve MEF services
            _logger.LogInfo("RunWelcomeServiceAsync: resolving IRegistryManager...");
            var registryManager = VsUtils.ResolveMefDependency<IRegistryManager>(sp);
            if (registryManager is null)
            {
                _logger.LogWarning("RunWelcomeServiceAsync: IRegistryManager not available, skipping.");
                return;
            }
            _logger.LogInfo("RunWelcomeServiceAsync: IRegistryManager resolved OK.");

            _logger.LogInfo("RunWelcomeServiceAsync: resolving IVersionProvider...");
            var versionProvider = VsUtils.ResolveMefDependency<IVersionProvider>(sp);
            if (versionProvider is null)
            {
                _logger.LogWarning("RunWelcomeServiceAsync: IVersionProvider not available, skipping.");
                return;
            }
            _logger.LogInfo("RunWelcomeServiceAsync: IVersionProvider resolved OK. Version=" + versionProvider.GetExtensionVersion());

            _logger.LogInfo("RunWelcomeServiceAsync: resolving IFileSystemForIDE...");
            var fileSystem = VsUtils.ResolveMefDependency<IFileSystemForIDE>(sp);
            if (fileSystem is null)
            {
                _logger.LogWarning("RunWelcomeServiceAsync: IFileSystemForIDE not available, skipping.");
                return;
            }
            _logger.LogInfo("RunWelcomeServiceAsync: IFileSystemForIDE resolved OK.");

            _logger.LogInfo("RunWelcomeServiceAsync: resolving IIdeScope...");
            var ideScope = VsUtils.ResolveMefDependency<IIdeScope>(sp);
            if (ideScope is null)
            {
                _logger.LogWarning("RunWelcomeServiceAsync: IIdeScope not available, skipping.");
                return;
            }
            _logger.LogInfo("RunWelcomeServiceAsync: IIdeScope resolved OK.");

            // Create the dialog service (manual creation, not MEF-exported)
            _logger.LogInfo("RunWelcomeServiceAsync: resolving IVsUIShell...");
            var vsUiShell = sp.GetService(typeof(SVsUIShell)) as IVsUIShell;
            if (vsUiShell is null)
            {
                _logger.LogWarning("RunWelcomeServiceAsync: IVsUIShell not available, skipping.");
                return;
            }
            _logger.LogInfo("RunWelcomeServiceAsync: IVsUIShell resolved OK.");

            var monitoringService = ideScope.MonitoringService;
            var dialogService = new VsWizardDialogService(vsUiShell, monitoringService);

            _logger.LogInfo("RunWelcomeServiceAsync: creating WelcomeService...");
            var welcomeService = new WelcomeService(
                registryManager, versionProvider, dialogService, fileSystem);

            _logger.LogInfo("RunWelcomeServiceAsync: calling OnIdeScopeActivityStarted...");
            welcomeService.OnIdeScopeActivityStarted(ideScope);
            _logger.LogInfo("RunWelcomeServiceAsync: OnIdeScopeActivityStarted returned (dialog scheduled with 7s delay).");
        }
        catch (Exception ex)
        {
            _logger.LogException(ex, "RunWelcomeServiceAsync: Failed");
        }
    }

    private async Task WaitForSolutionLoadAsync(CancellationToken cancellationToken)
    {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        var solution = await GetServiceAsync(typeof(SVsSolution)) as IVsSolution;
        if (solution is null)
            return;

        // Poll until the solution is open. ProvideAutoLoad(BackgroundLoad) guarantees the
        // solution exists by the time InitializeAsync runs, but projects may still be loading
        // in the background. We use a brief polling loop as a practical gate.
        const int maxAttempts = 40; // ~10 seconds
        for (int i = 0; i < maxAttempts; i++)
        {
            // The VSPSROPID_IsOpen value is 0x0000000B per the VS SDK headers.
            solution.GetProperty(0x0000000B, out var isOpen);
            if (isOpen is true)
                return;

            await Task.Delay(250, cancellationToken).ConfigureAwait(false);
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
        }

        _logger.LogInfo("WaitForSolutionLoadAsync: max attempts reached, proceeding anyway.");
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && _analyticsTransmitter is IAsyncDisposable d)
        {
            ThreadHelper.JoinableTaskFactory.Run(() => d.DisposeAsync().AsTask());
        }
        base.Dispose(disposing);
    }
}
