#nullable disable

namespace Reqnroll.IdeSupport.LSP.Core.Tests.Bindings;

public class ProjectBindingRegistryFindBindingAtLocationTests
{
    private static ProjectStepDefinitionBinding CreateBinding(SourceLocation location, string methodName = "MyStep") =>
        new(ScenarioBlock.Given, new Regex("^my step$"), null,
            new ProjectBindingImplementation(methodName, null, location));

    private static ProjectBindingRegistry RegistryWith(params ProjectStepDefinitionBinding[] bindings) =>
        new(bindings, Array.Empty<ProjectHookBinding>(), 0);

    [Fact]
    public void Exact_line_and_column_match_resolves()
    {
        var binding = CreateBinding(new SourceLocation("Steps.cs", 10, 5));
        var registry = RegistryWith(binding);

        registry.FindBindingAtLocation(new SourceLocation("Steps.cs", 10, 5))
            .Should().BeSameAs(binding);
    }

    [Fact]
    public void Same_line_different_column_still_resolves()
    {
        // Column is intentionally ignored — a click anywhere on the method identifier's line
        // should resolve, not just the exact recorded column.
        var binding = CreateBinding(new SourceLocation("Steps.cs", 10, 5));
        var registry = RegistryWith(binding);

        registry.FindBindingAtLocation(new SourceLocation("Steps.cs", 10, 30))
            .Should().BeSameAs(binding);
    }

    [Theory]
    [InlineData(8)]
    [InlineData(9)]
    public void Click_on_the_attribute_line_above_the_method_identifier_resolves(int attributeLine)
    {
        // The [Given] attribute is typically 1-2 lines above the discovered method identifier
        // (Roslyn-path) or method-body start (connector-path) — F2 rename is invoked from the
        // attribute, so it must resolve, not just the exact recorded line (issue #107).
        var binding = CreateBinding(new SourceLocation("Steps.cs", 10, 5));
        var registry = RegistryWith(binding);

        registry.FindBindingAtLocation(new SourceLocation("Steps.cs", attributeLine, 5))
            .Should().BeSameAs(binding);
    }

    [Fact]
    public void More_than_two_lines_before_the_recorded_line_does_not_resolve()
    {
        var binding = CreateBinding(new SourceLocation("Steps.cs", 10, 5));
        var registry = RegistryWith(binding);

        registry.FindBindingAtLocation(new SourceLocation("Steps.cs", 7, 5))
            .Should().BeNull();
    }

    [Fact]
    public void Line_within_the_recorded_end_line_resolves()
    {
        var binding = CreateBinding(new SourceLocation("Steps.cs", 10, 5, sourceFileEndLine: 15));
        var registry = RegistryWith(binding);

        registry.FindBindingAtLocation(new SourceLocation("Steps.cs", 13, 1))
            .Should().BeSameAs(binding);
    }

    [Fact]
    public void Line_past_the_recorded_end_line_does_not_resolve()
    {
        var binding = CreateBinding(new SourceLocation("Steps.cs", 10, 5, sourceFileEndLine: 15));
        var registry = RegistryWith(binding);

        registry.FindBindingAtLocation(new SourceLocation("Steps.cs", 16, 1))
            .Should().BeNull();
    }

    [Fact]
    public void Different_file_does_not_resolve()
    {
        var binding = CreateBinding(new SourceLocation("Steps.cs", 10, 5));
        var registry = RegistryWith(binding);

        registry.FindBindingAtLocation(new SourceLocation("OtherSteps.cs", 10, 5))
            .Should().BeNull();
    }

    [Fact]
    public void File_comparison_is_case_insensitive()
    {
        var binding = CreateBinding(new SourceLocation("Steps.cs", 10, 5));
        var registry = RegistryWith(binding);

        registry.FindBindingAtLocation(new SourceLocation("STEPS.CS", 10, 5))
            .Should().BeSameAs(binding);
    }

    [Fact]
    public void No_binding_with_a_source_location_returns_null()
    {
        var binding = new ProjectStepDefinitionBinding(ScenarioBlock.Given, new Regex("^my step$"), null,
            new ProjectBindingImplementation("MyStep", null, null));
        var registry = RegistryWith(binding);

        registry.FindBindingAtLocation(new SourceLocation("Steps.cs", 10, 5))
            .Should().BeNull();
    }
}
