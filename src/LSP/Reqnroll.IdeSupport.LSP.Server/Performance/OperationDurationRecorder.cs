using OmniSharp.Extensions.LanguageServer.Protocol;
using Reqnroll.IdeSupport.Common.Logging;
using Reqnroll.IdeSupport.LSP.Server.Hosting;
using Reqnroll.IdeSupport.LSP.Server.Telemetry;
using Reqnroll.IdeSupport.LSP.Server.Tracing;

namespace Reqnroll.IdeSupport.LSP.Server.Performance;

/// <summary>
/// Default <see cref="IOperationDurationRecorder"/>. Writes a structured, grep-able
/// <c>PERF</c> line to the existing log file on every call (the unsampled local record), and
/// optionally emits a sampled <c>PerfSample</c> telemetry event for field P95 aggregation.
/// </summary>
/// <remarks>
/// Privacy: the log line may carry the document URI for local diagnosis, but the telemetry
/// event must not — it carries only the operation label, the duration, a coarse bucket and the
/// IDE client, never a path or file content.
/// </remarks>
public sealed class OperationDurationRecorder : IOperationDurationRecorder
{
    /// <summary>Telemetry event name. Listed in the public telemetry inventory.</summary>
    public const string PerfSampleEventName = "PerfSample";

    private readonly IIdeSupportLogger _logger;
    private readonly ClientIdeContext _ide;
    private readonly ILspTelemetryService? _telemetry;
    private readonly IPerfTelemetrySampler _sampler;
    private readonly ITraceService? _trace;

    public OperationDurationRecorder(
        IIdeSupportLogger logger,
        ClientIdeContext ide,
        ILspTelemetryService? telemetry = null,
        IPerfTelemetrySampler? sampler = null,
        ITraceService? trace = null)
    {
        _logger = logger;
        _ide = ide;
        _telemetry = telemetry;
        _sampler = sampler ?? PerfTelemetrySampler.FromEnvironment();
        _trace = trace;
    }

    public IDisposable Measure(string operation, DocumentUri? uri = null)
        => new Scope(this, operation, uri);

    public void Record(string operation, double elapsedMs, DocumentUri? uri = null)
    {
        // Primary sink: the existing log file. Verbose so it is off in normal runs and on when
        // diagnostics are enabled. The "PERF " prefix makes it greppable for offline analysis.
        _logger.LogVerbose(() => uri is null
            ? $"PERF op={operation} ms={elapsedMs:F1}"
            : $"PERF op={operation} ms={elapsedMs:F1} uri={uri}");

        // F41: mirror the same measurement as a $/logTrace notification (a no-op unless the
        // client opted into tracing via InitializeParams.Trace or $/setTrace). The URI only goes
        // into the verbose detail, matching the log line's own privacy posture.
        _trace?.Trace(
            $"{operation}: {elapsedMs:F1}ms",
            uri is null ? null : () => uri.ToString());

        // Secondary sink: sampled telemetry metric — no URI/path (privacy).
        if (_telemetry is not null && _sampler.ShouldSample())
        {
            _telemetry.SendEvent(PerfSampleEventName, new Dictionary<string, object?>
            {
                ["Operation"] = operation,
                ["DurationMs"] = (long)Math.Round(elapsedMs),
                ["DurationBucket"] = Bucket(elapsedMs),
                ["IDEClient"] = _ide.Ide,
            });
        }
    }

    /// <summary>Coarse latency buckets for cheap field aggregation without exposing raw paths.</summary>
    internal static string Bucket(double ms) => ms switch
    {
        <= 10 => "<=10",
        <= 25 => "<=25",
        <= 50 => "<=50",
        <= 100 => "<=100",
        <= 250 => "<=250",
        <= 500 => "<=500",
        <= 1000 => "<=1000",
        <= 5000 => "<=5000",
        _ => ">5000",
    };

    private sealed class Scope : IDisposable
    {
        private readonly OperationDurationRecorder _owner;
        private readonly string _operation;
        private readonly DocumentUri? _uri;
        private readonly long _startTimestamp;
        private bool _disposed;

        public Scope(OperationDurationRecorder owner, string operation, DocumentUri? uri)
        {
            _owner = owner;
            _operation = operation;
            _uri = uri;
            _startTimestamp = Stopwatch.GetTimestamp();
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _owner.Record(_operation, Stopwatch.GetElapsedTime(_startTimestamp).TotalMilliseconds, _uri);
        }
    }
}
