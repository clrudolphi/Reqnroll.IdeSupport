#nullable enable

using Reqnroll.IdeSupport.LSP.Core.Discovery;

namespace Reqnroll.IdeSupport.LSP.Core.Matching;

/// <summary>
/// The Binding Match Service of section 3 of the LSP IDE Support design: a cache of step
/// binding matches keyed by feature document id, plus a reverse index from binding source
/// locations back to the feature steps that resolve to them.
/// </summary>
/// <remarks>
/// The cache is populated whenever a feature document is (re)parsed (see
/// <c>GherkinDocumentTaggerService</c>) and invalidated when the binding registry changes.
/// It is URI-agnostic: the server layer keys entries by <c>DocumentUri.ToString()</c>,
/// matching <c>IDocumentBufferService</c>.
/// </remarks>
public interface IBindingMatchService
{
    /// <summary>Stores (replacing any prior entry for the same <see cref="FeatureBindingMatchSet.DocumentId"/>).</summary>
    void Store(FeatureBindingMatchSet matchSet);

    /// <summary>Returns the cached match set for a document, or <see cref="FeatureBindingMatchSet.Empty"/>.</summary>
    bool TryGet(string documentId, out FeatureBindingMatchSet matchSet);

    /// <summary>Drops the cached match set for a single document.</summary>
    void Invalidate(string documentId);

    /// <summary>Drops all cached match sets (e.g. on a full registry replacement).</summary>
    void InvalidateAll();

    /// <summary>
    /// Returns every cached feature step that resolves to a binding at <paramref name="bindingLocation"/>.
    /// Backs Find Usages (F14) and Code Lens usage counts (F18).
    /// </summary>
    IReadOnlyList<StepBindingMatch> FindUsages(SourceLocation bindingLocation);
}
