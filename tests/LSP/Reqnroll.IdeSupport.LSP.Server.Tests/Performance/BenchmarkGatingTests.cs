#nullable enable

using System;
using Reqnroll.IdeSupport.LSP.Server.Benchmarks.Latency;

namespace Reqnroll.IdeSupport.LSP.Server.Tests.Performance;

[Collection("non-parallel")]
public class BenchmarkGatingTests
{
    private static LatencySummary Summary(double p95, double p50) =>
        new("op", SampleCount: 100, MinMs: 0, MeanMs: p50, P50Ms: p50, P95Ms: p95, P99Ms: p95, MaxMs: p95);

    [Fact]
    public void Interactive_result_asserts_against_P95()
    {
        var target = PerfTargets.DefinitionCacheHit; // 100ms, InteractiveP95
        new OperationResult(target, Summary(p95: 90, p50: 10)).MeetsTarget.Should().BeTrue();
        new OperationResult(target, Summary(p95: 110, p50: 10)).MeetsTarget.Should().BeFalse();
    }

    [Fact]
    public void Interactive_result_reports_P95_as_the_measured_statistic()
    {
        var r = new OperationResult(PerfTargets.SemanticTokensFull, Summary(p95: 42, p50: 5));
        r.MeasuredStatistic.Should().Be("P95");
        r.MeasuredMs.Should().Be(42);
    }

    [Fact]
    public void Batch_result_asserts_against_median()
    {
        var target = PerfTargets.ColdStartScan; // 30000ms, Batch
        new OperationResult(target, Summary(p95: 40000, p50: 25000)).MeetsTarget.Should().BeTrue();
        new OperationResult(target, Summary(p95: 26000, p50: 31000)).MeetsTarget.Should().BeFalse();
        new OperationResult(target, Summary(p95: 40000, p50: 25000)).MeasuredStatistic.Should().Be("median");
    }

    [Theory]
    [InlineData("1", true)]
    [InlineData("true", true)]
    [InlineData("YES", true)]
    [InlineData("0", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void ReferenceMachine_reads_env_var_truthiness(string? value, bool expected)
    {
        var original = Environment.GetEnvironmentVariable(ReferenceMachine.ReferenceMachineEnvVar);
        try
        {
            Environment.SetEnvironmentVariable(ReferenceMachine.ReferenceMachineEnvVar, value);
            ReferenceMachine.FromEnvironment().AssertThresholds.Should().Be(expected);
        }
        finally
        {
            Environment.SetEnvironmentVariable(ReferenceMachine.ReferenceMachineEnvVar, original);
        }
    }

    [Fact]
    public void ReferenceMachine_assert_flag_enables_thresholds_regardless_of_env()
    {
        var original = Environment.GetEnvironmentVariable(ReferenceMachine.ReferenceMachineEnvVar);
        try
        {
            Environment.SetEnvironmentVariable(ReferenceMachine.ReferenceMachineEnvVar, null);
            ReferenceMachine.FromEnvironment(new[] { "--assert" }).AssertThresholds.Should().BeTrue();
            ReferenceMachine.FromEnvironment(new[] { "run" }).AssertThresholds.Should().BeFalse();
        }
        finally
        {
            Environment.SetEnvironmentVariable(ReferenceMachine.ReferenceMachineEnvVar, original);
        }
    }
}
