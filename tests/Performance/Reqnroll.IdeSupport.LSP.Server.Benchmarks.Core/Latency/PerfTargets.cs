#nullable enable

using System.Collections.Generic;

namespace Reqnroll.IdeSupport.LSP.Server.Benchmarks.Latency;

/// <summary>Whether a performance target is an interactive round-trip (P95) or a batch op (wall-clock).</summary>
public enum PerfTargetKind
{
    /// <summary>Interactive: assert the P95 percentile against the threshold.</summary>
    InteractiveP95,

    /// <summary>Batch/throughput: assert the median (or max) wall-clock against the threshold.</summary>
    Batch,
}

/// <summary>
/// One latency target from the architecture's Performance Requirements. <see cref="Operation"/> matches the operation
/// label used by the benchmark scenarios and the Layer 4 recorder, so field and synthetic numbers
/// line up.
/// </summary>
public sealed record PerfTarget(string Operation, double TargetMs, PerfTargetKind Kind, string Description);

/// <summary>
/// The architecture's Performance Requirements table, as code (see LSP-IDE-Support-Architecture.md).
/// The single source of truth for what the benchmark asserts on the reference machine.
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

    // Load-only operations: they participate in the editing-session burst to create realistic
    // contention, but the Performance Requirements table publishes no threshold for them, so
    // TargetMs is 0 (rendered "—", never asserted). Not part of <see cref="All"/>.
    public static readonly PerfTarget DocumentSymbol =
        new("textDocument/documentSymbol", 0, PerfTargetKind.InteractiveP95, "Document outline (session load)");

    public static readonly PerfTarget FoldingRange =
        new("textDocument/foldingRange", 0, PerfTargetKind.InteractiveP95, "Folding ranges (session load)");

    // Measured-but-unpublished operations (issue #119): field-instrumented (see #113/#118) but no
    // threshold exists yet in the architecture's §9 Performance Requirements table, so TargetMs is
    // 0 (rendered "—", never asserted) until one is proposed and published there. Not part of
    // <see cref="All"/>. Step Rename and Find Unused Step Definitions are workspace-wide operations
    // (Batch-classified — median wall-clock, like the discovery scenarios) rather than a per-request
    // percentile.
    public static readonly PerfTarget StepPrepareRename =
        new("textDocument/prepareRename", 0, PerfTargetKind.InteractiveP95, "Prepare rename validity check (F16)");

    public static readonly PerfTarget RenameTargets =
        new("reqnroll/renameTargets", 0, PerfTargetKind.InteractiveP95, "Rename target picker candidates (F16)");

    public static readonly PerfTarget StepRename =
        new("textDocument/rename", 0, PerfTargetKind.Batch, "Step rename, workspace-wide WorkspaceEdit (F16)");

    public static readonly PerfTarget FindUnusedStepDefinitions =
        new("reqnroll/findUnusedStepDefinitions", 0, PerfTargetKind.Batch, "Find unused step definitions, workspace-wide scan (F15)");

    public static readonly PerfTarget SemanticTokensDelta =
        new("textDocument/semanticTokens/full/delta", 0, PerfTargetKind.InteractiveP95, "Semantic tokens delta pull");

    /// <summary>All performance targets, in table order.</summary>
    public static readonly IReadOnlyList<PerfTarget> All = new[]
    {
        SemanticTokensFull, CompletionKeyword, CompletionStep, DefinitionCacheHit, PublishDiagnostics,
        RoslynReDiscovery, ReflectionDiscovery, ColdStartScan,
    };
}
