using Reqnroll.IdeSupport.LSP.Core.Document;
using Reqnroll.IdeSupport.LSP.Core.Matching;
using Reqnroll.VisualStudio.VsxStubs.LspStubs;

namespace Reqnroll.IdeSupport.LSP.Core.Tests.Matching;

public class BindingMatchServiceTests
{
    private const string Uri = "file:///c:/proj/feature1.feature";

    private readonly IDeveroomLogger _logger = Substitute.For<IDeveroomLogger>();
    private readonly IMonitoringService _monitoringService = Substitute.For<IMonitoringService>();
    private readonly IDeveroomConfigurationProvider _configProvider = Substitute.For<IDeveroomConfigurationProvider>();

    public BindingMatchServiceTests()
    {
        _configProvider.GetConfiguration().Returns(new DeveroomConfiguration());
    }

    // ── helpers ────────────────────────────────────────────────────────────────

    private static ProjectStepDefinitionBinding GivenBinding(
        string pattern, string method = "MyStep", string file = "Steps.cs", int line = 5) =>
        new(ScenarioBlock.Given,
            new Regex("^" + Regex.Escape(pattern) + "$"),
            null,
            new ProjectBindingImplementation(method, null, new SourceLocation(file, line, 1)));

    private static ProjectBindingRegistry RegistryWith(params ProjectStepDefinitionBinding[] bindings) =>
        new(bindings, Array.Empty<ProjectHookBinding>(), 0);

    private IReadOnlyCollection<DeveroomTag> ParseTags(string text, ProjectBindingRegistry registry)
    {
        var parser = new DeveroomTagParser(_logger, _monitoringService, _configProvider);
        return parser.Parse(new StubGherkinTextSnapshot(text), registry);
    }

    private FeatureBindingMatchSet BuildSet(string text, ProjectBindingRegistry registry, int? version = 1)
    {
        var tags = ParseTags(text, registry);
        return FeatureBindingMatchSet.FromTags(Uri, version, registry.Version, tags);
    }

    private const string DefinedFeature =
        "Feature: F\nScenario: S\n    Given my step\n";

    private const string UndefinedFeature =
        "Feature: F\nScenario: S\n    Given no such step\n";

    // ── FromTags / FeatureBindingMatchSet ───────────────────────────────────────

    [Fact]
    public void FromTags_captures_a_defined_step_match()
    {
        var set = BuildSet(DefinedFeature, RegistryWith(GivenBinding("my step")));

        set.Steps.Should().ContainSingle();
        set.Steps[0].IsDefined.Should().BeTrue();
        set.Defined.Should().ContainSingle();
        set.Undefined.Should().BeEmpty();
    }

    [Fact]
    public void FromTags_captures_an_undefined_step_match()
    {
        var set = BuildSet(UndefinedFeature, RegistryWith(GivenBinding("my step")));

        set.Steps.Should().ContainSingle();
        set.Steps[0].IsUndefined.Should().BeTrue();
        set.Undefined.Should().ContainSingle();
        set.Defined.Should().BeEmpty();
    }

    [Fact]
    public void FindAt_returns_the_step_whose_span_contains_the_offset()
    {
        var set = BuildSet(DefinedFeature, RegistryWith(GivenBinding("my step")));
        var step = set.Steps[0];

        set.FindAt(step.Range.Start).Should().BeSameAs(step);
        set.FindAt(step.Range.End - 1).Should().BeSameAs(step);
        set.FindAt(step.Range.End).Should().BeNull();          // end is exclusive
        set.FindAt(0).Should().BeNull();                       // before the step text
    }

    [Fact]
    public void Defined_step_exposes_its_binding_source_location()
    {
        var set = BuildSet(DefinedFeature, RegistryWith(GivenBinding("my step", file: "Steps.cs", line: 5)));

        set.Steps[0].BindingLocations
            .Should().ContainSingle()
            .Which.SourceFile.Should().Be("Steps.cs");
    }

    [Fact]
    public void Empty_set_has_no_steps_and_FindAt_is_null()
    {
        FeatureBindingMatchSet.Empty.Steps.Should().BeEmpty();
        FeatureBindingMatchSet.Empty.FindAt(0).Should().BeNull();
    }

    // ── BindingMatchService cache ───────────────────────────────────────────────

    [Fact]
    public void Store_then_TryGet_returns_the_set()
    {
        var sut = new BindingMatchService();
        var set = BuildSet(DefinedFeature, RegistryWith(GivenBinding("my step")));

        sut.Store(set);

        sut.TryGet(Uri, out var found).Should().BeTrue();
        found.Should().BeSameAs(set);
    }

    [Fact]
    public void TryGet_unknown_document_returns_false_and_Empty()
    {
        var sut = new BindingMatchService();

        sut.TryGet("file:///nope.feature", out var found).Should().BeFalse();
        found.Should().BeSameAs(FeatureBindingMatchSet.Empty);
    }

    [Fact]
    public void Store_replaces_the_prior_set_for_the_same_document()
    {
        var sut = new BindingMatchService();
        var first = BuildSet(DefinedFeature, RegistryWith(GivenBinding("my step")), version: 1);
        var second = BuildSet(UndefinedFeature, RegistryWith(GivenBinding("my step")), version: 2);

        sut.Store(first);
        sut.Store(second);

        sut.TryGet(Uri, out var found).Should().BeTrue();
        found.Should().BeSameAs(second);
        found.DocumentVersion.Should().Be(2);
    }

    [Fact]
    public void Invalidate_drops_the_document_entry()
    {
        var sut = new BindingMatchService();
        sut.Store(BuildSet(DefinedFeature, RegistryWith(GivenBinding("my step"))));

        sut.Invalidate(Uri);

        sut.TryGet(Uri, out _).Should().BeFalse();
    }

    [Fact]
    public void InvalidateAll_clears_every_entry()
    {
        var sut = new BindingMatchService();
        sut.Store(BuildSet(DefinedFeature, RegistryWith(GivenBinding("my step"))));

        sut.InvalidateAll();

        sut.TryGet(Uri, out _).Should().BeFalse();
    }

    // ── reverse index (FindUsages) ──────────────────────────────────────────────

    [Fact]
    public void FindUsages_returns_steps_bound_to_the_given_source_location()
    {
        var sut = new BindingMatchService();
        sut.Store(BuildSet(DefinedFeature, RegistryWith(GivenBinding("my step", file: "Steps.cs", line: 5))));

        // Column differs intentionally — usages are matched by file + line only.
        var usages = sut.FindUsages(new SourceLocation("Steps.cs", 5, 99));

        usages.Should().ContainSingle();
    }

    [Fact]
    public void FindUsages_returns_nothing_for_an_unrelated_location()
    {
        var sut = new BindingMatchService();
        sut.Store(BuildSet(DefinedFeature, RegistryWith(GivenBinding("my step", file: "Steps.cs", line: 5))));

        sut.FindUsages(new SourceLocation("Other.cs", 1, 1)).Should().BeEmpty();
    }

    [Fact]
    public void FindUsages_null_location_returns_empty()
    {
        var sut = new BindingMatchService();
        sut.Store(BuildSet(DefinedFeature, RegistryWith(GivenBinding("my step"))));

        sut.FindUsages(null!).Should().BeEmpty();
    }

    [Fact]
    public void FindUsages_each_result_carries_the_feature_document_id()
    {
        var sut = new BindingMatchService();
        sut.Store(BuildSet(DefinedFeature, RegistryWith(GivenBinding("my step", file: "Steps.cs", line: 5))));

        var usage = sut.FindUsages(new SourceLocation("Steps.cs", 5, 1)).Single();

        usage.FeatureDocumentId.Should().Be(Uri);
    }

    [Fact]
    public void FindUsages_finds_matches_across_multiple_documents()
    {
        const string secondUri = "file:///c:/proj/feature2.feature";
        var sut = new BindingMatchService();
        var registry = RegistryWith(GivenBinding("my step", file: "Steps.cs", line: 5));

        sut.Store(BuildSet(DefinedFeature, registry));

        // Manually build a second set keyed on a different document URI.
        var tags2      = ParseTags(DefinedFeature, registry);
        var secondSet  = FeatureBindingMatchSet.FromTags(secondUri, documentVersion: 1, registry.Version, tags2);
        sut.Store(secondSet);

        var usages = sut.FindUsages(new SourceLocation("Steps.cs", 5, 1));

        usages.Should().HaveCount(2);
        usages.Select(u => u.FeatureDocumentId).Should()
              .BeEquivalentTo([Uri, secondUri]);
    }

    [Fact]
    public void FindUsages_uses_case_insensitive_path_comparison_on_source_file()
    {
        var sut = new BindingMatchService();
        sut.Store(BuildSet(DefinedFeature, RegistryWith(GivenBinding("my step", file: "Steps.cs", line: 5))));

        // Windows paths are case-insensitive.
        var usages = sut.FindUsages(new SourceLocation("STEPS.CS", 5, 1));

        usages.Should().ContainSingle();
    }

    [Fact]
    public void FindUsages_does_not_return_undefined_steps()
    {
        var sut = new BindingMatchService();
        sut.Store(BuildSet(UndefinedFeature, RegistryWith(GivenBinding("my step"))));

        // The step text doesn't match — it has no BindingLocations, so no location to match.
        sut.FindUsages(new SourceLocation("Steps.cs", 5, 1)).Should().BeEmpty();
    }
}
