#nullable enable

namespace Reqnroll.IdeSupport.LSP.Server.Benchmarks.Latency;

/// <summary>
/// Pairs a measured <see cref="LatencySummary"/> with its §9 <see cref="PerfTarget"/> and derives
/// the comparison verdict. For interactive targets the asserted statistic is P95; for batch targets
/// it is the median (P50) wall-clock.
/// </summary>
public sealed record OperationResult(PerfTarget Target, LatencySummary Summary)
{
    /// <summary>The statistic compared against the target (P95 for interactive, P50 for batch).</summary>
    public double MeasuredMs => Target.Kind == PerfTargetKind.InteractiveP95 ? Summary.P95Ms : Summary.P50Ms;

    /// <summary>The name of the asserted statistic, for reporting.</summary>
    public string MeasuredStatistic => Target.Kind == PerfTargetKind.InteractiveP95 ? "P95" : "median";

    /// <summary>True when the measured statistic is within the §9 threshold.</summary>
    public bool MeetsTarget => MeasuredMs <= Target.TargetMs;
}
