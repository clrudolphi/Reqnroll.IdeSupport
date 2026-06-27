#nullable enable

using System.Collections.Generic;

namespace Reqnroll.IdeSupport.LSP.Server.Benchmarks.Latency;

/// <summary>Whether a §9 target is an interactive round-trip (P95) or a batch op (wall-clock).</summary>
public enum PerfTargetKind
{
    /// <summary>Interactive: assert the P95 percentile against the threshold.</summary>
    InteractiveP95,

    /// <summary>Batch/throughput: assert the median (or max) wall-clock against the threshold.</summary>
    Batch,
}

/// <summary>
/// One §9 Performance Requirements latency target. <see cref="Operation"/> matches the operation
/// label used by the benchmark scenarios and the Layer 4 recorder, so field and synthetic numbers
/// line up.
/// </summary>
public sealed record PerfTarget(string Operation, double TargetMs, PerfTargetKind Kind, string Description);

/// <summary>
/// The §9 Performance Requirements table, as code. The single source of truth for what the
/// benchmark asserts on the reference machine.
/// </summary>
public static class PerfTargets
{
    public static readonly PerfTarget SemanticTokensFull =
        new("textDocument/semanticTokens/full", 100, PerfTargetKind.InteractiveP95, "Semantic tokens from last didChange");

    public static readonly PerfTarget CompletionKeyword =
        new("textDocument/completion#keyword", 50, PerfTargetKind.InteractiveP95, "Keyword completion (F7)");

    public static readonly PerfTarget CompletionStep =
        new("textDocument/completion#step", 150, PerfTargetKind.InteractiveP95, "Step completion (F8)");

    public static readonly PerfTarget DefinitionCacheHit =
        new("textDocument/definition", 100, PerfTargetKind.InteractiveP95, "Go to definition, cache hit (F5)");

    public static readonly PerfTarget PublishDiagnostics =
        new("textDocument/publishDiagnostics", 500, PerfTargetKind.InteractiveP95, "Diagnostics push from end of debounce");

    public static readonly PerfTarget RoslynReDiscovery =
        new("discovery/roslyn-single-cs", 2000, PerfTargetKind.Batch, "Roslyn binding re-discovery, single .cs file");

    public static readonly PerfTarget ReflectionDiscovery =
        new("discovery/reflection-post-build", 10000, PerfTargetKind.Batch, "Reflection binding discovery, post-build");

    public static readonly PerfTarget ColdStartScan =
        new("workspace/cold-start-scan", 30000, PerfTargetKind.Batch, "Initial workspace scan, cold start");

    /// <summary>All §9 targets, in table order.</summary>
    public static readonly IReadOnlyList<PerfTarget> All = new[]
    {
        SemanticTokensFull, CompletionKeyword, CompletionStep, DefinitionCacheHit, PublishDiagnostics,
        RoslynReDiscovery, ReflectionDiscovery, ColdStartScan,
    };
}
