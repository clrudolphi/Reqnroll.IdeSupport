#nullable disable
using Gherkin;
using GherkinLocation = Gherkin.Ast.Location;

namespace Reqnroll.IdeSupport.LSP.Core.Tests.Bindings;

/// <summary>
/// Thin wrapper around the shared <see cref="ProjectBindingRegistryTestsBase"/>
/// from <c>Reqnroll.IdeSupport.LSP.TestStubs</c>, adding feature-structure building methods
/// that require <c>InternalsVisibleTo</c> access to <c>DeveroomTag.AddChild</c>.
/// </summary>
public abstract class ProjectBindingRegistryTestsBase
    : global::Reqnroll.IdeSupport.LSP.TestStubs.ProjectBindingRegistryTestsBase
{
    private DeveroomTag CreateFeatureStructure(string[] featureTags, string[] scenarioTags,
        string[] scenarioOutlineTags = null, string[] soHeaders = null, string[][] soCells = null,
        bool includeScenario = true, bool includeOutline = true, string[] outlineExamplesTags = null)
    {
        featureTags = featureTags ?? new string[0];
        scenarioTags = scenarioTags ?? new string[0];
        scenarioOutlineTags = scenarioOutlineTags ?? new string[0];
        outlineExamplesTags = outlineExamplesTags ?? new string[0];
        soHeaders = soHeaders ?? new[] { "param1", "param2" };
        soCells = soCells ?? new[] { new[] { "r1c1", "r1c2" }, new[] { "r2c1", "r2c2" } };

        var scenarioDefinitions = new List<StepsContainer>();
        scenarioDefinitions.Add(new Background(new GherkinLocation(0, 0), "Background", "my background", null, new Step[0]));
        if (includeScenario)
            scenarioDefinitions.Add(new SingleScenario(scenarioTags.Select(t => new Tag(new GherkinLocation(0, 0), t)).ToArray(), new GherkinLocation(0, 0),
                "Scenario", "my scenario", null, new Step[0]));
        if (includeOutline)
            scenarioDefinitions.Add(new ScenarioOutline(scenarioOutlineTags.Select(t => new Tag(new GherkinLocation(0, 0), t)).ToArray(),
                new GherkinLocation(0, 0), "Scenario Outline", "my scenario outline", null, new Step[0], new[]
                {
                    new Examples(outlineExamplesTags.Select(t => new Tag(new GherkinLocation(0, 0), t)).ToArray(), new GherkinLocation(0, 0), "Examples",
                        "my examples",
                        null, new TableRow(new GherkinLocation(0, 0), soHeaders.Select(h => new TableCell(new GherkinLocation(0, 0), h)).ToArray()),
                        soCells.Select(r => new TableRow(new GherkinLocation(0, 0), r.Select(c => new TableCell(new GherkinLocation(0, 0), c)).ToArray()))
                            .ToArray())
                }));

        var feature = new Feature(featureTags.Select(t => new Tag(new GherkinLocation(0, 0), t)).ToArray(), new GherkinLocation(0, 0), "en", "Feature",
            "my feature", null, scenarioDefinitions.ToArray());
        var featureTag = new DeveroomTag(DeveroomTagTypes.FeatureBlock, default, feature);
        var backgroundTag = new DeveroomTag(DeveroomTagTypes.ScenarioDefinitionBlock, default,
            feature.Children.OfType<Background>().First());
        featureTag.AddChild(backgroundTag);
        if (includeScenario)
        {
            var scenarioTag = new DeveroomTag(DeveroomTagTypes.ScenarioDefinitionBlock, default,
                feature.Children.OfType<Scenario>().First());
            featureTag.AddChild(scenarioTag);
        }

        if (includeOutline)
        {
            var scenarioOutlineTag = new DeveroomTag(DeveroomTagTypes.ScenarioDefinitionBlock, default,
                feature.Children.OfType<ScenarioOutline>().First());
            featureTag.AddChild(scenarioOutlineTag);
        }

        return featureTag;
    }

    protected IGherkinDocumentContext CreateScenarioContext(string[] featureTags, params string[] scenarioTags)
    {
        var featureTag = CreateFeatureStructure(featureTags, scenarioTags);
        return featureTag.ChildTags.First(t => t.Data is Scenario);
    }

    protected IGherkinDocumentContext CreateScenarioOutlineContext(string[] featureTags, string[] scenarioOutlineTags,
        string soHeader, string[] soCells, string[] outlineExamplesTags = null)
    {
        var featureTag = CreateFeatureStructure(featureTags, null, scenarioOutlineTags, new[] { soHeader },
            soCells.Select(r => new[] { r }).ToArray(), outlineExamplesTags: outlineExamplesTags);
        return featureTag.ChildTags.First(t => t.Data is ScenarioOutline);
    }

    protected IGherkinDocumentContext CreateScenarioOutlineContext(string[] featureTags = null,
        string[] scenarioOutlineTags = null, string[] soHeaders = null, string[][] soCells = null)
    {
        var featureTag = CreateFeatureStructure(featureTags, null, scenarioOutlineTags, soHeaders, soCells);
        return featureTag.ChildTags.First(t => t.Data is ScenarioOutline);
    }

    protected IGherkinDocumentContext CreateBackgroundContext(string[] featureTags = null, string[] scenarioTags = null,
        string[] scenarioOutlineTags = null, string[] outlineExamplesTags = null)
    {
        var featureTag = CreateFeatureStructure(featureTags, scenarioTags, scenarioOutlineTags,
            outlineExamplesTags: outlineExamplesTags);
        return featureTag.ChildTags.First(t => t.Data is Background);
    }

    protected IGherkinDocumentContext CreateEmptyFileBackgroundContext(string[] featureTags)
    {
        var featureTag = CreateFeatureStructure(featureTags, null, includeScenario: false, includeOutline: false);
        return featureTag.ChildTags.First(t => t.Data is Background);
    }
}
