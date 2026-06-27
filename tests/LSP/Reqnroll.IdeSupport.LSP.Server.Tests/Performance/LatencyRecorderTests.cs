#nullable enable

using System;
using System.Linq;
using Reqnroll.IdeSupport.LSP.Server.Benchmarks.Latency;

namespace Reqnroll.IdeSupport.LSP.Server.Tests.Performance;

public class LatencyRecorderTests
{
    [Fact]
    public void Summarize_computes_min_max_mean_and_percentiles()
    {
        var rec = new LatencyRecorder("op");
        foreach (var v in Enumerable.Range(1, 100)) rec.Add(v); // 1..100

        var s = rec.Summarize();

        s.Operation.Should().Be("op");
        s.SampleCount.Should().Be(100);
        s.MinMs.Should().Be(1);
        s.MaxMs.Should().Be(100);
        s.MeanMs.Should().BeApproximately(50.5, 0.001);
        // Nearest-rank: P50 = sample[ceil(0.50*100)=50] = 50; P95 = sample[95] = 95; P99 = 99.
        s.P50Ms.Should().Be(50);
        s.P95Ms.Should().Be(95);
        s.P99Ms.Should().Be(99);
    }

    [Fact]
    public void Percentile_is_order_independent()
    {
        var rec = new LatencyRecorder("op");
        foreach (var v in new[] { 9, 1, 7, 3, 5, 2, 8, 4, 6, 10 }) rec.Add(v);

        rec.Summarize().P95Ms.Should().Be(10);   // ceil(0.95*10)=10 → largest
    }

    [Fact]
    public void Percentile_single_sample_returns_that_sample()
        => LatencyRecorder.Percentile(new[] { 42.0 }, 0.95).Should().Be(42.0);

    [Fact]
    public void Percentile_clamps_quantile_into_unit_range()
    {
        var sorted = new[] { 1.0, 2.0, 3.0 };
        LatencyRecorder.Percentile(sorted, 1.5).Should().Be(3.0);  // clamp to 1.0 → last
        LatencyRecorder.Percentile(sorted, -0.5).Should().Be(1.0); // clamp to 0.0 → first
    }

    [Fact]
    public void Summarize_throws_when_no_samples()
    {
        var rec = new LatencyRecorder("op");
        Action act = () => rec.Summarize();
        act.Should().Throw<InvalidOperationException>();
    }
}
