#nullable enable

using Reqnroll.IdeSupport.LSP.Core.Discovery;
using Reqnroll.IdeSupport.LSP.Core.Editor.Services.Parsing.GherkinDocuments;

namespace Reqnroll.IdeSupport.LSP.Core.Matching;

/// <summary>
/// The immutable set of step binding matches for one feature document, cached against
/// <c>(DocumentId, DocumentVersion, RegistryVersion)</c>. This is the value stored by
/// <see cref="IBindingMatchService"/> and queried by Go to Definition (F5), the diagnostics
/// aggregator (F3) and find-usages (F14/F18).
/// </summary>
public sealed class FeatureBindingMatchSet
{
    public static readonly FeatureBindingMatchSet Empty =
        new(string.Empty, null, 0, Array.Empty<StepBindingMatch>());

    public FeatureBindingMatchSet(
        string documentId,
        int? documentVersion,
        int registryVersion,
        IReadOnlyList<StepBindingMatch> steps)
    {
        DocumentId = documentId ?? throw new ArgumentNullException(nameof(documentId));
        DocumentVersion = documentVersion;
        RegistryVersion = registryVersion;
        Steps = steps ?? throw new ArgumentNullException(nameof(steps));
    }

    public string DocumentId { get; }

    /// <summary>The feature document version these matches were computed for, when known.</summary>
    public int? DocumentVersion { get; }

    /// <summary>The <see cref="ProjectBindingRegistry.Version"/> these matches were computed against.</summary>
    public int RegistryVersion { get; }

    public IReadOnlyList<StepBindingMatch> Steps { get; }

    public IEnumerable<StepBindingMatch> Undefined => Steps.Where(s => s.IsUndefined);
    public IEnumerable<StepBindingMatch> Defined => Steps.Where(s => s.IsDefined);
    public IEnumerable<StepBindingMatch> Ambiguous => Steps.Where(s => s.IsAmbiguous);

    /// <summary>The step whose text span contains <paramref name="offset"/>, or null. Used by F5.</summary>
    public StepBindingMatch? FindAt(int offset) => Steps.FirstOrDefault(s => s.Contains(offset));

    /// <summary>
    /// Builds a match set from the flattened tag collection produced by <c>DeveroomTagParser</c>.
    /// Each <see cref="DeveroomTagTypes.DefinedStep"/> / <see cref="DeveroomTagTypes.UndefinedStep"/>
    /// tag carries the step text span as its range and the computed <see cref="MatchResult"/> as its data;
    /// this method projects those into <see cref="StepBindingMatch"/> entries.
    /// </summary>
    /// <remarks>
    /// A single step can emit both a DefinedStep and an UndefinedStep tag (e.g. a scenario outline
    /// whose example rows partly match), but both reference the same <see cref="MatchResult"/> at the
    /// same span; such duplicates are collapsed so the set holds one entry per step.
    /// </remarks>
    public static FeatureBindingMatchSet FromTags(
        string documentId,
        int? documentVersion,
        int registryVersion,
        IEnumerable<DeveroomTag> tags)
    {
        var byStart = new Dictionary<int, StepBindingMatch>();

        foreach (var tag in tags)
        {
            if (tag.Type is not (DeveroomTagTypes.DefinedStep or DeveroomTagTypes.UndefinedStep))
                continue;
            if (tag.Data is not MatchResult match)
                continue;

            // Collapse the DefinedStep/UndefinedStep pair a single step may emit: same span, same result.
            if (!byStart.ContainsKey(tag.Range.Start))
                byStart[tag.Range.Start] = new StepBindingMatch(documentId, tag.Range, match);
        }

        var steps = byStart.Values.OrderBy(s => s.Range.Start).ToArray();
        return new FeatureBindingMatchSet(documentId, documentVersion, registryVersion, steps);
    }
}
