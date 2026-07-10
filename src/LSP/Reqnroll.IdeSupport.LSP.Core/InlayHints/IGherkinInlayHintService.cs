using Reqnroll.IdeSupport.LSP.Core.Matching;

namespace Reqnroll.IdeSupport.LSP.Core.InlayHints;

/// <summary>Projects a feature file's binding match cache into inline hint annotations (F23).</summary>
public interface IGherkinInlayHintService
{
    /// <summary>Gets or sets the build.</summary>
    IReadOnlyList<GherkinInlayHint> Build(FeatureBindingMatchSet matchSet);
}
