#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;

namespace Reqnroll.IdeSupport.LSP.Server.Benchmarks.Latency;

/// <summary>
/// Collects per-invocation latency samples for one operation and computes summary percentiles.
/// Bespoke (not BenchmarkDotNet) because the interactive performance targets are percentile-of-latency
/// over a stateful async server, not throughput of an isolated micro-op — see the implementation
/// plan, A1.
/// </summary>
public sealed class LatencyRecorder
{
    private readonly List<double> _samplesMs = new();

    public string Operation { get; }

    public LatencyRecorder(string operation) => Operation = operation;

    public void Add(double ms) => _samplesMs.Add(ms);

    public int Count => _samplesMs.Count;

    /// <summary>
    /// Computes the percentile summary over the collected samples using the
    /// nearest-rank method (P95 = the sample at <c>ceil(0.95·N)</c>, 1-based).
    /// </summary>
    public LatencySummary Summarize()
    {
        if (_samplesMs.Count == 0)
            throw new InvalidOperationException($"No samples recorded for '{Operation}'.");

        var sorted = _samplesMs.OrderBy(x => x).ToArray();
        return new LatencySummary(
            Operation: Operation,
            SampleCount: sorted.Length,
            MinMs: sorted[0],
            MeanMs: sorted.Average(),
            P50Ms: Percentile(sorted, 0.50),
            P95Ms: Percentile(sorted, 0.95),
            P99Ms: Percentile(sorted, 0.99),
            MaxMs: sorted[^1]);
    }

    /// <summary>Nearest-rank percentile over an ascending-sorted array. <paramref name="q"/> in [0,1].</summary>
    public static double Percentile(IReadOnlyList<double> sortedAscending, double q)
    {
        if (sortedAscending.Count == 0)
            throw new ArgumentException("Empty sample set.", nameof(sortedAscending));
        if (sortedAscending.Count == 1) return sortedAscending[0];

        q = Math.Clamp(q, 0.0, 1.0);
        // 1-based rank = ceil(q * N), clamped to [1, N]; index = rank - 1.
        var rank = (int)Math.Ceiling(q * sortedAscending.Count);
        rank = Math.Clamp(rank, 1, sortedAscending.Count);
        return sortedAscending[rank - 1];
    }
}

/// <summary>Immutable percentile summary for one operation.</summary>
public sealed record LatencySummary(
    string Operation,
    int SampleCount,
    double MinMs,
    double MeanMs,
    double P50Ms,
    double P95Ms,
    double P99Ms,
    double MaxMs);
