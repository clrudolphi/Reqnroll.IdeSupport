#nullable enable
namespace Reqnroll.VisualStudio.VsxStubs.ProjectSystem;

public class StubAnalyticsTransmitter : ITelemetryTransmitter, IEnumerable<ITelemetryEvent>
{
    private readonly IIdeSupportLogger _logger;

    public StubAnalyticsTransmitter(IIdeSupportLogger logger)
    {
        _logger = logger;
    }

    private ConcurrentBag<ITelemetryEvent> Events { get; } = new();

    public void TransmitEvent(ITelemetryEvent runtimeEvent)
    {
        Events.Add(runtimeEvent);
        _logger.LogVerbose(runtimeEvent.EventName);
    }

    public void TransmitFatalExceptionEvent(Exception exception, bool isFatal)
    {
        //nop
    }

    public void TransmitExceptionEvent(Exception exception, IEnumerable<KeyValuePair<string, object>> additionalProps)
    {
        //nop
    }

    public IEnumerator<ITelemetryEvent> GetEnumerator() => Events.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => Events.GetEnumerator();
}
