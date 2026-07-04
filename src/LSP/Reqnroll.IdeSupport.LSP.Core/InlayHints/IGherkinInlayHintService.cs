using Reqnroll.IdeSupport.LSP.Core.Matching;

namespace Reqnroll.IdeSupport.LSP.Core.InlayHints;

/// <summary>Projects a feature file's binding match cache into inline hint annotations (F23).</summary>
public interface IGherkinInlayHintService
{
    IReadOnlyList<GherkinInlayHint> Build(FeatureBindingMatchSet matchSet);
}
