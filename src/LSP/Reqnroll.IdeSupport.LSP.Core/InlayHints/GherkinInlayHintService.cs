using Reqnroll.IdeSupport.LSP.Core.Bindings;
using Reqnroll.IdeSupport.LSP.Core.Matching;

namespace Reqnroll.IdeSupport.LSP.Core.InlayHints;

/// <inheritdoc cref="IGherkinInlayHintService"/>
public sealed class GherkinInlayHintService : IGherkinInlayHintService
{
    /// <summary>Gets or sets the build.</summary>
    public IReadOnlyList<GherkinInlayHint> Build(FeatureBindingMatchSet matchSet)
    {
        var hints = new List<GherkinInlayHint>();

        foreach (var step in matchSet.Steps)
        {
            var result = step.Result;
            if (result is null)
                continue;

            var ambiguous = DistinctBindings(result, MatchResultType.Ambiguous);
            if (ambiguous.Count > 0)
            {
                hints.Add(new GherkinInlayHint(
                    step.Range,
                    $"→ {ambiguous.Count} matches",
                    DescribeCandidates(ambiguous),
                    GherkinInlayHintKind.Ambiguous));
                continue;
            }

            // A Scenario Outline/Background template step's single MatchResult merges every
            // example row (FeatureBindingMatchSet.FromTags): more than one *distinct* Defined
            // binding here means different rows resolved to different step definitions, not that
            // any one row's own text was ambiguous.
            var defined = DistinctBindings(result, MatchResultType.Defined);
            if (defined.Count > 1)
            {
                hints.Add(new GherkinInlayHint(
                    step.Range,
                    $"→ {defined.Count} bindings",
                    DescribeCandidates(defined),
                    GherkinInlayHintKind.Templated));
            }
            else if (defined.Count == 1)
            {
                var binding = defined[0];
                hints.Add(new GherkinInlayHint(
                    step.Range,
                    $"→ {ShortName(binding)}",
                    Describe(binding),
                    GherkinInlayHintKind.Binding));
            }
            // Otherwise every item is Undefined — the diagnostic already covers that; no hint.
        }

        return hints;
    }

    private static List<ProjectStepDefinitionBinding> DistinctBindings(MatchResult result, MatchResultType type) =>
        result.Items
            .Where(i => i.Type == type && i.MatchedStepDefinition is not null)
            .Select(i => i.MatchedStepDefinition!)
            .Distinct()
            .ToList();

    private static string DescribeCandidates(IReadOnlyList<ProjectStepDefinitionBinding> bindings) =>
        string.Join("\n", bindings.Select(Describe));

    /// <summary>The type/method name without the namespace prefix, e.g. "CalculatorSteps.AddNumbers".</summary>
    private static string ShortName(ProjectStepDefinitionBinding binding)
    {
        var method = binding.Implementation?.Method ?? string.Empty;
        var segments = method.Split('.');
        return segments.Length >= 2
            ? $"{segments[segments.Length - 2]}.{segments[segments.Length - 1]}"
            : method;
    }

    /// <summary>The full signature shown in the tooltip, e.g. "N.CalculatorSteps.AddNumbers(int, int)".</summary>
    private static string Describe(ProjectStepDefinitionBinding binding)
    {
        var method = binding.Implementation?.Method ?? "(unknown)";
        var parameters = binding.Implementation?.ParameterTypes ?? Array.Empty<string>();
        return $"{method}({string.Join(", ", parameters)})";
    }
}
