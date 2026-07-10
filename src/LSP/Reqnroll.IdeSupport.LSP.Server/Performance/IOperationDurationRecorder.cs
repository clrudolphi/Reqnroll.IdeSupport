using OmniSharp.Extensions.LanguageServer.Protocol;

namespace Reqnroll.IdeSupport.LSP.Server.Performance;

/// <summary>
/// Records the wall-clock duration of an LSP protocol operation for the architecture's Performance
/// Verification "Layer 4" field instrumentation: real-world P95 measured in the live server,
/// emitted via the existing logging path and (optionally, sampled) as a telemetry metric.
/// </summary>
/// <remarks>
/// This is the single cross-cutting sink invoked at each handler boundary. It exists because the
/// four interactive performance targets live on three different registration rails — manual
/// <c>OnRequest</c> delegates (<c>semanticTokens/full</c>), OmniSharp <c>AddHandler</c> handlers
/// (<c>completion</c>, <c>definition</c>) and a MediatR notification push
/// (<c>publishDiagnostics</c>) — so no single MediatR pipeline behavior can cover them all.
/// </remarks>
public interface IOperationDurationRecorder
{
    /// <summary>
    /// Starts a timing scope; the elapsed time is recorded when the returned handle is disposed.
    /// Usage: <c>using var _ = recorder.Measure("textDocument/completion", uri);</c>
    /// </summary>
    IDisposable Measure(string operation, DocumentUri? uri = null);

    /// <summary>
    /// Records an already-measured duration for <paramref name="operation"/>. Use this overload
    /// when the operation label is only known after the work runs (e.g. keyword vs. step
    /// completion).
    /// </summary>
    void Record(string operation, double elapsedMs, DocumentUri? uri = null);
}
