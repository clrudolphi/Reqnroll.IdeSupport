#nullable disable
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Reqnroll.IdeSupport.Common;
using Reqnroll.IdeSupport.Common.Logging;
using Reqnroll.IdeSupport.Common.ProjectSystem;
using Reqnroll.IdeSupport.Common.Telemetry;
using Reqnroll.IdeSupport.VisualStudio.Common;
using Reqnroll.IdeSupport.VisualStudio.Package.ProjectSystem;
using Reqnroll.IdeSupport.VisualStudio.SDKIntegration;
using System.Collections.Concurrent;
using System.ComponentModel.Composition;
using System.Threading.Tasks;

namespace Reqnroll.IdeSupport.VisualStudio.ProjectSystem;

[Export(typeof(IIdeScope))]
[Export(typeof(IVsIdeScope))]
public class VsIdeScope : IVsIdeScope
{
    private readonly CancellationTokenSource _backgroundTaskTokenSource = new();
    private readonly ConcurrentDictionary<string, VsProjectScope>
        _projectScopes = new(StringComparer.OrdinalIgnoreCase);

    private bool _activityStarted;

    [ImportingConstructor]
    public VsIdeScope([Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider,
        ITelemetryService telemetryService,
        IFileSystemForIDE fileSystem,
        Reqnroll.IdeSupport.VisualStudio.Diagnostics.IdeSupportCompositeLogger compositeLogger)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        Logger = compositeLogger;
        ServiceProvider = serviceProvider;
        TelemetryService = telemetryService;
        FileSystem = fileSystem;

        Dte = (DTE) serviceProvider.GetService(typeof(DTE));

        Logger.LogVerbose("Creating IDE Scope");

        IsSolutionLoaded = Dte.Solution.IsOpen;
    }

    public IServiceProvider ServiceProvider { get; }
    public DTE Dte { get; }

    public bool IsSolutionLoaded { get; private set; }

    public IIdeSupportLogger Logger { get; }
    public ITelemetryService TelemetryService { get; }
    public IIdeActions Actions { get; }
    public IFileSystemForIDE FileSystem { get; }

    public void FireAndForget(Func<Task> action, Action<Exception> onException,
        [CallerMemberName] string callerName = "???")
    {
        action().FileAndForget($"vs/Reqnroll/{nameof(FireAndForget)}/{callerName}",
            "Error on a background task in Reqnroll",
            exception =>
            {
                Logger.LogException(TelemetryService, exception, $"Called from {callerName}");
                onException(exception);
                return true;
            });
    }

    public void FireAndForgetOnBackgroundThread(Func<CancellationToken, Task> action,
        [CallerMemberName] string callerName = "???")
    {
        _ = Task.Factory.StartNew(async () =>
            {
                try
                {
                    ThreadHelper.ThrowIfOnUIThread(callerName);
                    await action(_backgroundTaskTokenSource.Token);
                }
                catch (Exception e)
                {
                    Logger.LogException(TelemetryService, e, $"Called from {callerName}");
                }
            },
            _backgroundTaskTokenSource.Token,
            TaskCreationOptions.LongRunning,
            TaskScheduler.Default
        );
    }

    public async Task RunOnUiThreadAsync(Action action)
    {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
        action();
    }

    public void Dispose()
    {
        _backgroundTaskTokenSource.Cancel();
        _backgroundTaskTokenSource.Dispose();
    }

    public IProjectScope GetProjectScope(Project project)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        if (project == null ||
            !VsUtils.IsSolutionProject(project))
            return new VoidProjectScope(this);

        var projectId = GetProjectId(project);
        var projectScope = _projectScopes.GetOrAdd(projectId, id => CreateProjectScope(id, project));
        return projectScope;
    }

    private void OnActivityStarted()
    {
        if (_activityStarted)
            return;

        _activityStarted = true;
    }

    private string GetProjectId(Project project)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        return project.FullName;
    }

    private VsProjectScope CreateProjectScope(string id, Project project)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        OnActivityStarted();
        Logger.LogInfo($"Initializing project: {project.Name}");
        var projectScope = new VsProjectScope(id, project, this);
        projectScope.InitializeServices();
        return projectScope;
    }

    private bool HasFeatureFiles(Project project)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        try
        {
            if (!VsUtils.IsSolutionProject(project))
                return false;
            return VsUtils.GetPhysicalFileProjectItems(project)
                .Any(pi => FileSystemHelper.IsOfType(VsUtils.GetFilePath(pi), ".feature"));
        }
        catch (Exception e)
        {
            Logger.LogDebugException(e);
            return false;
        }
    }
}
