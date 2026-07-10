#nullable enable

using System;
using System.Globalization;

namespace Reqnroll.IdeSupport.LSP.Server.Performance;

/// <summary>
/// Decides whether a given <c>PerfSample</c> telemetry event should be emitted. Field perf
/// instrumentation fires on every interactive request, so the telemetry metric is sampled to
/// bound event volume — the local log line (always written at verbose) is the unsampled record.
/// </summary>
public interface IPerfTelemetrySampler
{
    bool ShouldSample();
}

/// <summary>
/// Probabilistic sampler. The rate is read from the <c>REQNROLL_PERF_TELEMETRY_SAMPLE</c>
/// environment variable (a fraction in <c>[0,1]</c>); when unset or unparsable it defaults to
/// <see cref="DefaultSampleRate"/> = 0, i.e. perf telemetry is <b>opt-in</b>. Set it to e.g.
/// <c>0.05</c> to emit ~5% of samples. The host-side opt-out gate still applies downstream.
/// </summary>
public sealed class PerfTelemetrySampler : IPerfTelemetrySampler
{
    public const string SampleRateEnvVar = "REQNROLL_PERF_TELEMETRY_SAMPLE";

    /// <summary>Opt-in by default: no perf telemetry is emitted unless a rate is configured.</summary>
    public const double DefaultSampleRate = 0.0;

    private readonly double _rate;
    private readonly Random _random;

    public PerfTelemetrySampler(double rate, Random? random = null)
    {
        _rate = Math.Clamp(rate, 0.0, 1.0);
        _random = random ?? Random.Shared;
    }

    public bool ShouldSample()
    {
        if (_rate <= 0.0) return false;
        if (_rate >= 1.0) return true;
        return _random.NextDouble() < _rate;
    }

    public static PerfTelemetrySampler FromEnvironment()
    {
        var raw = Environment.GetEnvironmentVariable(SampleRateEnvVar);
        var rate = double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var r)
            ? r
            : DefaultSampleRate;
        return new PerfTelemetrySampler(rate);
    }
}
