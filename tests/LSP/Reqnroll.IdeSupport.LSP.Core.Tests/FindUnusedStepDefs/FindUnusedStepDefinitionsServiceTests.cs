using Reqnroll.IdeSupport.LSP.Core.FindUnusedStepDefs;

namespace Reqnroll.IdeSupport.LSP.Core.Tests.FindUnusedStepDefs;

public class FindUnusedStepDefinitionsServiceTests
{
    private readonly IBindingMatchService _matchService = Substitute.For<IBindingMatchService>();
    private readonly IIdeSupportLogger _logger = Substitute.For<IIdeSupportLogger>();

    private FindUnusedStepDefinitionsService CreateSut() => new(_matchService, _logger);

    // ── Helper factory methods ─────────────────────────────────────────────────

    private static ProjectStepDefinitionBinding MakeBinding(
        string sourceFile,
        int line          = 1,
        int column        = 1,
        string method     = "StepDefinitions.GivenSomething()",
        string expression = "something")
    {
        var loc  = new SourceLocation(sourceFile, line, column);
        var impl = new ProjectBindingImplementation(method, null, loc);
        return new ProjectStepDefinitionBinding(
            ScenarioBlock.Given,
            new Regex("^something$"),
            null,
            impl,
            expression);
    }

    /// <summary>
    /// Returns two bindings on the <em>same</em> C# method (shared
    /// <see cref="ProjectBindingImplementation"/>, same source location), mirroring what
    /// the connector produces when a method carries multiple step-attribute decorations:
    /// <code>
    /// [Given("first expression")]
    /// [When("second expression")]
    /// public void MyMethod() { … }
    /// </code>
    /// Both <see cref="ProjectStepDefinitionBinding"/> objects reference the same
    /// <see cref="ProjectBindingImplementation"/> instance, so they share
    /// <see cref="SourceLocation"/>, class name, and method name.
    /// </summary>
    private static (ProjectStepDefinitionBinding First, ProjectStepDefinitionBinding Second)
        MakeTwoExpressionsOnSameMethod(
            string sourceFile,
            int    line        = 10,
            string method      = "StepDefs.MultiAttributeMethod()",
            string expression1 = "first expression",
            string expression2 = "second expression")
    {
        var loc  = new SourceLocation(sourceFile, line, 1);
        var impl = new ProjectBindingImplementation(method, null, loc);  // shared instance

        var b1 = new ProjectStepDefinitionBinding(
            ScenarioBlock.Given,
            new Regex("^" + Regex.Escape(expression1) + "$"),
            null, impl, expression1);

        var b2 = new ProjectStepDefinitionBinding(
            ScenarioBlock.When,
            new Regex("^" + Regex.Escape(expression2) + "$"),
            null, impl, expression2);

        return (b1, b2);
    }

    /// <summary>
    /// Returns a <see cref="StepBindingMatch"/> whose <c>Result.Items</c> record
    /// <paramref name="binding"/> as the matched step definition. The service checks
    /// <c>MatchedStepDefinition.Expression</c> to determine per-expression usage, so the
    /// returned match must carry the real binding object, not a bare <c>MatchResult.NoMatch</c>.
    /// </summary>
    private static StepBindingMatch MakeMatchForBinding(ProjectStepDefinitionBinding binding)
    {
        var snapshot = new LspTextSnapshot(
            "file:///any.feature", 1,
            "Feature: F\nScenario: S\n    Given x\n");
        var range  = GherkinRange.FromPoint(snapshot, 33, 1);
        var item   = MatchResultItem.CreateMatch(binding, ParameterMatch.NotMatch);
        var result = MatchResult.CreateMultiMatch(new[] { item });
        return new StepBindingMatch("file:///any.feature", range, result);
    }

    private static (string ProjectName, ProjectBindingRegistry Registry)
        MakeEntry(string projectName, params ProjectStepDefinitionBinding[] bindings) =>
        (projectName, ProjectBindingRegistry.FromBindings(bindings));

    // ── Empty workspace ────────────────────────────────────────────────────────

    [Fact]
    public void No_projects_returns_empty_result()
    {
        var result = CreateSut().FindUnusedStepDefinitions(Array.Empty<(string, ProjectBindingRegistry)>());

        result.Should().BeEmpty();
    }

    [Fact]
    public void Project_with_no_bindings_returns_empty_result()
    {
        var result = CreateSut().FindUnusedStepDefinitions(new[] { MakeEntry("A") });

        result.Should().BeEmpty();
    }

    // ── Unused detection ──────────────────────────────────────────────────────

    [Fact]
    public void Binding_with_no_usages_is_reported()
    {
        var binding = MakeBinding("/ws/Steps.cs", line: 10);
        _matchService.FindUsages(Arg.Any<SourceLocation>(), Arg.Any<IReadOnlyCollection<ProjectOwner>>())
                     .Returns(Array.Empty<StepBindingMatch>());

        var result = CreateSut().FindUnusedStepDefinitions(new[] { MakeEntry("A", binding) });

        result.Should().ContainSingle();
    }

    [Fact]
    public void Binding_with_usages_is_not_reported()
    {
        var binding = MakeBinding("/ws/Steps.cs", line: 10);
        // The match carries the binding's expression so the per-expression usage check passes.
        _matchService.FindUsages(Arg.Any<SourceLocation>(), Arg.Any<IReadOnlyCollection<ProjectOwner>>())
                     .Returns(new[] { MakeMatchForBinding(binding) });

        var result = CreateSut().FindUnusedStepDefinitions(new[] { MakeEntry("A", binding) });

        result.Should().BeEmpty();
    }

    // ── Result fields ────────────────────────────────────────────────────────

    [Fact]
    public void Reports_project_name_from_registry_entry()
    {
        var binding = MakeBinding("/ws/Steps.cs");
        _matchService.FindUsages(Arg.Any<SourceLocation>(), Arg.Any<IReadOnlyCollection<ProjectOwner>>())
                     .Returns(Array.Empty<StepBindingMatch>());

        var result = CreateSut().FindUnusedStepDefinitions(new[] { MakeEntry("MyProject", binding) });

        result.Single().ProjectName.Should().Be("MyProject");
    }

    [Fact]
    public void Reports_class_name_parsed_from_method()
    {
        var binding = MakeBinding("/ws/Steps.cs", method: "StepDefs.GivenSomething()");
        _matchService.FindUsages(Arg.Any<SourceLocation>(), Arg.Any<IReadOnlyCollection<ProjectOwner>>())
                     .Returns(Array.Empty<StepBindingMatch>());

        var result = CreateSut().FindUnusedStepDefinitions(new[] { MakeEntry("A", binding) });

        result.Single().ClassName.Should().Be("StepDefs");
    }

    [Fact]
    public void Reports_method_name_parsed_from_method_without_params()
    {
        var binding = MakeBinding("/ws/Steps.cs", method: "StepDefs.GivenSomething(int, string)");
        _matchService.FindUsages(Arg.Any<SourceLocation>(), Arg.Any<IReadOnlyCollection<ProjectOwner>>())
                     .Returns(Array.Empty<StepBindingMatch>());

        var result = CreateSut().FindUnusedStepDefinitions(new[] { MakeEntry("A", binding) });

        result.Single().MethodName.Should().Be("GivenSomething");
    }

    [Fact]
    public void Reports_method_name_from_namespaced_roslyn_method()
    {
        // Roslyn path produces "Namespace.ClassName.MethodName" (no params)
        var binding = MakeBinding("/ws/Steps.cs", method: "MyApp.Steps.GivenSomething");
        _matchService.FindUsages(Arg.Any<SourceLocation>(), Arg.Any<IReadOnlyCollection<ProjectOwner>>())
                     .Returns(Array.Empty<StepBindingMatch>());

        var result = CreateSut().FindUnusedStepDefinitions(new[] { MakeEntry("A", binding) });

        var item = result.Single();
        item.ClassName.Should().Be("Steps");
        item.MethodName.Should().Be("GivenSomething");
    }

    [Fact]
    public void Reports_binding_expression()
    {
        var binding = MakeBinding("/ws/Steps.cs", expression: "the sum is {int}");
        _matchService.FindUsages(Arg.Any<SourceLocation>(), Arg.Any<IReadOnlyCollection<ProjectOwner>>())
                     .Returns(Array.Empty<StepBindingMatch>());

        var result = CreateSut().FindUnusedStepDefinitions(new[] { MakeEntry("A", binding) });

        result.Single().BindingExpression.Should().Be("the sum is {int}");
    }

    [Fact]
    public void Reports_source_file_from_source_location()
    {
        var binding = MakeBinding("/ws/MySteps.cs", line: 42);
        _matchService.FindUsages(Arg.Any<SourceLocation>(), Arg.Any<IReadOnlyCollection<ProjectOwner>>())
                     .Returns(Array.Empty<StepBindingMatch>());

        var result = CreateSut().FindUnusedStepDefinitions(new[] { MakeEntry("A", binding) });

        result.Single().SourceFile.Should().Be("/ws/MySteps.cs");
    }

    [Fact]
    public void Reports_1based_source_line_and_column_unchanged()
    {
        // 1-based → 0-based wire conversion is the Server handler's responsibility, not the
        // Core service's - the service returns the domain-native 1-based position.
        var binding = MakeBinding("/ws/Steps.cs", line: 10, column: 5);
        _matchService.FindUsages(Arg.Any<SourceLocation>(), Arg.Any<IReadOnlyCollection<ProjectOwner>>())
                     .Returns(Array.Empty<StepBindingMatch>());

        var result = CreateSut().FindUnusedStepDefinitions(new[] { MakeEntry("A", binding) });

        result.Single().SourceLine.Should().Be(10);
        result.Single().SourceColumn.Should().Be(5);
    }

    // ── Deduplication (same source location in multiple project registries) ────

    [Fact]
    public void Deduplicates_same_source_location_across_projects()
    {
        var binding = MakeBinding("/ws/Steps.cs", line: 10);
        var entryA  = MakeEntry("A", binding);
        var entryB  = MakeEntry("B", binding);
        _matchService.FindUsages(Arg.Any<SourceLocation>(), Arg.Any<IReadOnlyCollection<ProjectOwner>>())
                     .Returns(Array.Empty<StepBindingMatch>());

        var result = CreateSut().FindUnusedStepDefinitions(new[] { entryA, entryB });

        // Same source location → reported only once
        result.Should().ContainSingle();
    }

    [Fact]
    public void Reports_distinct_bindings_in_same_project()
    {
        var b1 = MakeBinding("/ws/Steps.cs", line: 10);
        var b2 = MakeBinding("/ws/Steps.cs", line: 20);
        _matchService.FindUsages(Arg.Any<SourceLocation>(), Arg.Any<IReadOnlyCollection<ProjectOwner>>())
                     .Returns(Array.Empty<StepBindingMatch>());

        var result = CreateSut().FindUnusedStepDefinitions(new[] { MakeEntry("A", b1, b2) });

        result.Should().HaveCount(2);
    }

    // ── FindUsages is called with no project filter (global intersection) ──────

    [Fact]
    public void Passes_null_project_filter_to_FindUsages()
    {
        var binding = MakeBinding("/ws/Steps.cs");
        _matchService.FindUsages(Arg.Any<SourceLocation>(), Arg.Any<IReadOnlyCollection<ProjectOwner>>())
                     .Returns(Array.Empty<StepBindingMatch>());

        CreateSut().FindUnusedStepDefinitions(new[] { MakeEntry("A", binding) });

        _matchService.Received(1).FindUsages(
            Arg.Any<SourceLocation>(),
            Arg.Is<IReadOnlyCollection<ProjectOwner>?>(f => f == null));
    }

    // ── Invalid bindings are skipped ──────────────────────────────────────────

    [Fact]
    public void Skips_bindings_with_no_source_location()
    {
        var impl    = new ProjectBindingImplementation("MyClass.MyMethod()", null, null!);
        var binding = new ProjectStepDefinitionBinding(
            ScenarioBlock.Given,
            new Regex("^x$"),
            null, impl, "x");

        var result = CreateSut().FindUnusedStepDefinitions(new[] { MakeEntry("A", binding) });

        result.Should().BeEmpty();
        _matchService.DidNotReceive()
                     .FindUsages(Arg.Any<SourceLocation>(), Arg.Any<IReadOnlyCollection<ProjectOwner>>());
    }

    [Fact]
    public void Skips_invalid_bindings_regex_null()
    {
        var loc     = new SourceLocation("/ws/Steps.cs", 1, 1);
        var impl    = new ProjectBindingImplementation("MyClass.MyMethod()", null, loc);
        var binding = new ProjectStepDefinitionBinding(
            ScenarioBlock.Given,
            null,    // null regex → IsValid == false
            null, impl, "x");

        var result = CreateSut().FindUnusedStepDefinitions(new[] { MakeEntry("A", binding) });

        result.Should().BeEmpty();
        _matchService.DidNotReceive()
                     .FindUsages(Arg.Any<SourceLocation>(), Arg.Any<IReadOnlyCollection<ProjectOwner>>());
    }

    // ── Invalid registry is skipped ───────────────────────────────────────────

    [Fact]
    public void Skips_invalid_registry()
    {
        var result = CreateSut().FindUnusedStepDefinitions(
            new[] { ("A", ProjectBindingRegistry.Invalid) });

        result.Should().BeEmpty();
    }

    // ── Multiple binding attributes on the same method ────────────────────────
    //
    // A C# method may carry more than one step attribute:
    //
    //   [Given("first expression")]
    //   [When("second expression")]
    //   public void MultiAttributeMethod() { … }
    //
    // The connector produces one ProjectStepDefinitionBinding per attribute, but all share
    // the SAME ProjectBindingImplementation (same source file and line).
    //
    // The service checks each expression independently:
    //   - FindUsages(loc) returns all steps matched by ANY expression at that location.
    //   - Per-expression filtering: a step is a usage of expression X only when
    //     its MatchResultItem.MatchedStepDefinition.Expression == X.
    //   - Each unused expression produces its own result row.
    //   - An expression that IS matched in a feature file is omitted from the results.
    //   - FindUsages is called once per source location (not once per expression):
    //     the service caches the result and re-uses it for each expression on the method.

    [Fact]
    public void Method_with_two_expressions_both_unused_reports_two_rows()
    {
        // Each unused expression on a multi-attribute method gets its own row.
        var (b1, b2) = MakeTwoExpressionsOnSameMethod("/ws/Steps.cs", line: 10,
            expression1: "first expression",
            expression2: "second expression");
        _matchService.FindUsages(Arg.Any<SourceLocation>(), Arg.Any<IReadOnlyCollection<ProjectOwner>>())
                     .Returns(Array.Empty<StepBindingMatch>());  // neither matched in any feature

        var result = CreateSut().FindUnusedStepDefinitions(new[] { MakeEntry("A", b1, b2) });

        result.Should().HaveCount(2, "each unused expression is a separate result row");
        result.Select(i => i.BindingExpression)
              .Should().BeEquivalentTo(new[] { "first expression", "second expression" });
    }

    [Fact]
    public void Method_with_two_expressions_both_unused_calls_FindUsages_once()
    {
        // Even though there are two binding objects, FindUsages must be called only once
        // per source location — the result is cached for the second expression.
        var (b1, b2) = MakeTwoExpressionsOnSameMethod("/ws/Steps.cs", line: 10);
        _matchService.FindUsages(Arg.Any<SourceLocation>(), Arg.Any<IReadOnlyCollection<ProjectOwner>>())
                     .Returns(Array.Empty<StepBindingMatch>());

        CreateSut().FindUnusedStepDefinitions(new[] { MakeEntry("A", b1, b2) });

        _matchService.Received(1)
                     .FindUsages(Arg.Any<SourceLocation>(), Arg.Any<IReadOnlyCollection<ProjectOwner>>());
    }

    [Fact]
    public void Method_with_one_expression_used_reports_only_unused_expression()
    {
        // b1 ("first expression") is matched in a feature file.
        // b2 ("second expression") has no matches.
        // Only b2 should appear in the results.
        var (b1, b2) = MakeTwoExpressionsOnSameMethod("/ws/Steps.cs", line: 10,
            expression1: "first expression",
            expression2: "second expression");

        // FindUsages returns a match whose MatchedStepDefinition is b1 (expression1 used only).
        _matchService.FindUsages(Arg.Any<SourceLocation>(), Arg.Any<IReadOnlyCollection<ProjectOwner>>())
                     .Returns(new[] { MakeMatchForBinding(b1) });

        var result = CreateSut().FindUnusedStepDefinitions(new[] { MakeEntry("A", b1, b2) });

        result.Should().ContainSingle("only the unused expression is reported; the used one is omitted");
        result.Single().BindingExpression.Should().Be("second expression");
    }

    [Fact]
    public void Method_with_all_expressions_used_is_not_reported()
    {
        var (b1, b2) = MakeTwoExpressionsOnSameMethod("/ws/Steps.cs", line: 10,
            expression1: "first expression",
            expression2: "second expression");

        // FindUsages returns matches for both expressions.
        _matchService.FindUsages(Arg.Any<SourceLocation>(), Arg.Any<IReadOnlyCollection<ProjectOwner>>())
                     .Returns(new[] { MakeMatchForBinding(b1), MakeMatchForBinding(b2) });

        var result = CreateSut().FindUnusedStepDefinitions(new[] { MakeEntry("A", b1, b2) });

        result.Should().BeEmpty();
    }

    [Fact]
    public void Mixed_multi_expression_methods_reports_only_unused_expressions()
    {
        // Method A (line 10): two expressions, neither used → 2 rows.
        // Method B (line 20): two expressions, first is used, second is not → 1 row (second).
        var (aB1, aB2) = MakeTwoExpressionsOnSameMethod("/ws/Steps.cs", line: 10,
            method:      "StepDefs.UnusedMethod()",
            expression1: "unused expr A1",
            expression2: "unused expr A2");

        var (bB1, bB2) = MakeTwoExpressionsOnSameMethod("/ws/Steps.cs", line: 20,
            method:      "StepDefs.PartiallyUsedMethod()",
            expression1: "used expr B1",
            expression2: "unused expr B2");

        var locA = aB1.Implementation!.SourceLocation!;
        var locB = bB1.Implementation!.SourceLocation!;

        // Method A: nothing matched.
        _matchService
            .FindUsages(
                Arg.Is<SourceLocation>(l => l.SourceFileLine == locA.SourceFileLine),
                Arg.Any<IReadOnlyCollection<ProjectOwner>>())
            .Returns(Array.Empty<StepBindingMatch>());

        // Method B: only bB1 ("used expr B1") is matched; bB2 has no usages.
        _matchService
            .FindUsages(
                Arg.Is<SourceLocation>(l => l.SourceFileLine == locB.SourceFileLine),
                Arg.Any<IReadOnlyCollection<ProjectOwner>>())
            .Returns(new[] { MakeMatchForBinding(bB1) });

        var result = CreateSut().FindUnusedStepDefinitions(
            new[] { MakeEntry("A", aB1, aB2, bB1, bB2) });

        // Expected: A1, A2 (method A both unused), B2 (method B's unused expression) = 3 rows.
        result.Should().HaveCount(3);
        result.Select(i => i.BindingExpression)
              .Should().BeEquivalentTo(
                  new[] { "unused expr A1", "unused expr A2", "unused expr B2" });
    }
}
