using Reqnroll.IdeSupport.Common.Logging;
using Reqnroll.IdeSupport.LSP.Server.Workspace;

namespace Reqnroll.IdeSupport.LSP.Server.Pipeline;

/// <inheritdoc cref="IFeatureRescanDebouncer"/>
public sealed class FeatureRescanDebouncer : IFeatureRescanDebouncer, IDisposable
{
    private const int DebounceMilliseconds = 500;

    private readonly IIdeSupportLogger _logger;
    private readonly ConcurrentDictionary<LspReqnrollProject, CancellationTokenSource> _pending = new();

    public FeatureRescanDebouncer(IIdeSupportLogger logger)
    {
        _logger = logger;
    }

    public void ScheduleRescan(LspReqnrollProject project, Func<CancellationToken, Task> rescanAsync)
    {
        var newCts = new CancellationTokenSource();

        // Cancel and dispose any pending rescan for this project before replacing it -- only the
        // most recently scheduled action for a project should ever run.
        _pending.AddOrUpdate(project, newCts, (_, existing) =>
        {
            existing.Cancel();
            existing.Dispose();
            return newCts;
        });

        _ = RunAfterDebounceAsync(project, rescanAsync, newCts);
    }

    private async Task RunAfterDebounceAsync(
        LspReqnrollProject project, Func<CancellationToken, Task> rescanAsync, CancellationTokenSource cts)
    {
        try
        {
            await Task.Delay(DebounceMilliseconds, cts.Token).ConfigureAwait(false);
            await rescanAsync(cts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Normal: a later edit superseded this scheduled rescan.
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"[{project.ProjectName}] Debounced feature rescan failed: {ex.Message}");
        }
        finally
        {
            // Only remove our own entry -- a newer ScheduleRescan call may already have replaced it.
            _pending.TryRemove(new KeyValuePair<LspReqnrollProject, CancellationTokenSource>(project, cts));
            cts.Dispose();
        }
    }

    public void Dispose()
    {
        foreach (var cts in _pending.Values)
        {
            cts.Cancel();
            cts.Dispose();
        }
        _pending.Clear();
    }
}
