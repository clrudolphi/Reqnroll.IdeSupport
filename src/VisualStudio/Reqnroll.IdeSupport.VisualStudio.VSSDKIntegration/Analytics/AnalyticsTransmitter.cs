using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Reqnroll.IdeSupport.Common.Telemetry;
using Reqnroll.IdeSupport.Common.Logging;

namespace Reqnroll.IdeSupport.VisualStudio.Analytics;

/// <summary>
/// MEF-exported Application Insights transmitter for the Visual Studio host.
/// <para>
/// This is the only component in the .NET solution that depends on
/// <c>Microsoft.ApplicationInsights</c>. The LSP server never transmits telemetry — it
/// emits <c>telemetry/event</c> notifications, which the VS client forwards to this
/// transmitter (see <c>TelemetryEventInterceptor</c>). Because pre-LSP/lifecycle events
/// (Welcome wizard, New Project wizard, install/upgrade) are raised before any server
/// exists, transmission is necessarily host-side, so each IDE owns its own transmitter
/// (VS in .NET here, VSCode in TypeScript, Rider on the JVM). The IDE-neutral contracts
/// (<see cref="ITelemetryTransmitter"/>, <see cref="ITelemetryEvent"/>) stay in
/// Core/Common so the cross-platform server's dependency graph never pulls in AppInsights.
/// </para>
/// </summary>
[Export(typeof(ITelemetryTransmitter))]
public class AnalyticsTransmitter : ITelemetryTransmitter, IAsyncDisposable
{
    private readonly TelemetryClient _telemetryClient;
    private readonly IEnableTelemetryChecker _enableAnalyticsChecker;
    private readonly IIdeSupportLogger? _logger;
    private readonly ITelemetryDebugLog _debugLog;

    [ImportingConstructor]
    public AnalyticsTransmitter(
        IEnableTelemetryChecker enableAnalyticsChecker,
        IUserUniqueIdStore userUniqueIdStore,
        IVersionProvider versionProvider,
        Reqnroll.IdeSupport.VisualStudio.Diagnostics.IdeSupportCompositeLogger? logger = null)
        : this(CreateClient(userUniqueIdStore, versionProvider), enableAnalyticsChecker, logger,
            TelemetryDebugLog.FromEnvironment())
    {
    }

    /// <summary>
    /// Test seam: inject a <see cref="TelemetryClient"/> backed by an in-memory channel so
    /// transmission can be asserted without contacting Application Insights, and an
    /// <see cref="ITelemetryDebugLog"/> to assert what the host mirrored.
    /// </summary>
    internal AnalyticsTransmitter(
        TelemetryClient telemetryClient,
        IEnableTelemetryChecker enableAnalyticsChecker,
        IIdeSupportLogger? logger = null,
        ITelemetryDebugLog? debugLog = null)
    {
        _telemetryClient = telemetryClient;
        _enableAnalyticsChecker = enableAnalyticsChecker;
        _logger = logger;
        _debugLog = debugLog ?? NullTelemetryDebugLog.Instance;
    }

    private static TelemetryClient CreateClient(IUserUniqueIdStore userStore, IVersionProvider versionProvider)
    {
        var config = new TelemetryConfiguration();
        var assembly = typeof(AnalyticsTransmitter).Assembly;
        var resourceName = assembly.GetManifestResourceNames()
            .Single(n => n.EndsWith("InstrumentationKey.txt", StringComparison.Ordinal));
        using var stream = assembly.GetManifestResourceStream(resourceName);
        using var reader = new StreamReader(stream!);
        config.ConnectionString = reader.ReadLine();
        var client = new TelemetryClient(config);
        client.Context.User.Id = userStore.GetUserId();
        client.Context.User.AccountId = userStore.GetUserId();
        client.Context.GlobalProperties["Ide"] = "Microsoft Visual Studio";
        client.Context.GlobalProperties["IdeVersion"] = versionProvider.GetVsVersion();
        client.Context.GlobalProperties["ExtensionVersion"] = versionProvider.GetExtensionVersion();
        return client;
    }

    public void TransmitEvent(ITelemetryEvent analyticsEvent)
    {
        var enabled = _enableAnalyticsChecker.IsEnabled();
        try
        {
            DumpAnalyticsEvent(analyticsEvent);
            if (!enabled)
            {
                // Mirror the event even when opted out — debugging needs to see what *would*
                // have been sent — recording that it was gated and not transmitted.
                _debugLog.Record("host", analyticsEvent.EventName, analyticsEvent.Properties,
                    enabled: false, transmitted: false);
                return;
            }

            var eventTelemetry = new EventTelemetry(analyticsEvent.EventName) { Timestamp = DateTime.UtcNow };
            foreach (var property in analyticsEvent.Properties)
            {
                eventTelemetry.Properties.Add(property.Key, property.Value?.ToString() ?? string.Empty);
            }
            _telemetryClient.TrackEvent(eventTelemetry);

            _debugLog.Record("host", analyticsEvent.EventName, analyticsEvent.Properties,
                enabled: true, transmitted: true);
        }
        catch (Exception ex)
        {
            _debugLog.Record("host", analyticsEvent.EventName, analyticsEvent.Properties,
                enabled: enabled, transmitted: false, error: ex.Message);
            TransmitExceptionEvent(ex, ImmutableDictionary<string, object>.Empty);
        }
    }

    public void TransmitExceptionEvent(Exception exception, IEnumerable<KeyValuePair<string, object>> additionalProps)
    {
        var isNormalError = IsNormalError(exception);
        if (isNormalError)
            TransmitException(exception, additionalProps);
        else
            TransmitFatalExceptionEvent(exception, true);
    }

    public void TransmitFatalExceptionEvent(Exception exception, bool isFatal)
    {
        var additionalProps = ImmutableDictionary.CreateBuilder<string, object>();
        if (isFatal)
            additionalProps.Add("IsFatal", isFatal.ToString());

        TransmitException(exception, additionalProps.ToImmutable());
    }

    private void TransmitException(Exception exception, IEnumerable<KeyValuePair<string, object>> additionalProps)
    {
        var additionalPropsArray = additionalProps.ToArray();
        var transmitted = false;
        string? transmitError = null;
        try
        {
            DumpAnalyticsException(exception, additionalPropsArray);

            var exceptionTelemetry = new ExceptionTelemetry(exception) { Timestamp = DateTime.UtcNow };
            foreach (var prop in additionalPropsArray)
            {
                exceptionTelemetry.Properties.Add(prop.Key, prop.Value?.ToString() ?? string.Empty);
            }
            _telemetryClient.TrackException(exceptionTelemetry);
            transmitted = true;
        }
        catch (Exception ex)
        {
            // catch all exceptions since we do not want to break the whole extension simply because data transmission failed
            transmitError = ex.Message;
            Debug.WriteLine(ex, "Error during transmitting analytics event.");
        }

        // Mirror the exception telemetry for debugging. The exception path is not gated by the
        // opt-out checker (hence enabled: null). `error` is a *transmission* failure, distinct from
        // the reported exception's own message, which is carried in props.
        _debugLog.Record("host", $"(exception) {exception.GetType().Name}",
            BuildExceptionProps(exception, additionalPropsArray),
            enabled: null, transmitted: transmitted, error: transmitError);
    }

    private static Dictionary<string, object?> BuildExceptionProps(
        Exception exception, KeyValuePair<string, object>[] additionalProps)
    {
        var props = new Dictionary<string, object?>
        {
            ["ExceptionType"] = exception.GetType().FullName,
            ["Message"] = exception.Message,
        };
        foreach (var p in additionalProps)
            props[p.Key] = p.Value;
        return props;
    }

    [Conditional("ANALYTICS_DEBUG")]
    private void DumpAnalyticsEvent(ITelemetryEvent analyticsEvent)
    {
        _logger?.LogVerbose(() => $"{analyticsEvent.EventName}: {string.Join(Environment.NewLine + "  ", analyticsEvent.Properties.Select(p => $"{p.Key}={p.Value}"))}");
    }

    [Conditional("ANALYTICS_DEBUG")]
    private void DumpAnalyticsException(Exception exception, IEnumerable<KeyValuePair<string, object>> additionalProps)
    {
        _logger?.LogVerbose(() => $"{exception.Message}: {string.Join(Environment.NewLine + "  ", additionalProps.Select(p => $"{p.Key}={p.Value}"))}");
    }

    private static bool IsNormalError(Exception exception)
    {
        if (exception is AggregateException aggregateException)
            return aggregateException.InnerExceptions.All(IsNormalError);
        return
            //exception is DeveroomConfigurationException ||
            exception is TimeoutException ||
            exception is TaskCanceledException ||
            exception is OperationCanceledException ||
            exception is HttpRequestException;
    }

    public async ValueTask DisposeAsync()
    {
        _telemetryClient.Flush();
        await Task.Delay(1000);
    }
}
