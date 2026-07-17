using System.Text.RegularExpressions;
using Reqnroll.IdeSupport.LSP.Core.Bindings;
using Reqnroll.IdeSupport.LSP.Core.Documents;
using Reqnroll.IdeSupport.LSP.Core.Matching;
using Reqnroll.IdeSupport.LSP.Core.Parsing.Gherkin;
using Reqnroll.IdeSupport.LSP.Server.Features.Rename;

namespace Reqnroll.IdeSupport.LSP.Server.Tests.Features.Rename;

public class RenameBindingResolverTests
{
    private const string CsPath = "/workspace/Steps.cs";

    private static ProjectStepDefinitionBinding MakeBinding(
        string expression, int methodLine, int? attributeSourceLine) =>
        new(
            ScenarioBlock.Given,
            new Regex("^" + Regex.Escape(expression) + "$"),
            null,
            new ProjectBindingImplementation($"Steps.Given_{methodLine}()", null, new SourceLocation(CsPath, methodLine, 9)),
            expression,
            attributeSourceLine: attributeSourceLine);

    // Regression test for issue #170/#176-adjacent: a class with two distinct step methods
    // declared close together used to be treated as one ambiguous "multi-attribute" method by a
    // crude "within 5 lines of any binding in the file" heuristic — invoking Rename Step from the
    // second method's attribute falsely offered a 2-candidate picker including the unrelated
    // first method, even though the two step expressions share nothing but parameter shape.
    [Fact]
    public void FindBindingsAtCSharpMethod_does_not_merge_two_distinct_nearby_methods()
    {
        // [Given("the first number is {int}")]   line 9  (attribute), line 10 (method)
        // public void GivenTheFirstNumberIs(int p0) { }
        //
        // [Given("the second num is {int}")]      line 15 (attribute), line 16 (method)
        // public void GivenTheSecondNumIs(int p0) { }
        var first = MakeBinding("the first number is {int}", methodLine: 10, attributeSourceLine: 9);
        var second = MakeBinding("the second num is {int}", methodLine: 16, attributeSourceLine: 15);
        var registry = ProjectBindingRegistry.FromBindings(new[] { first, second });

        // Cursor on the second method's attribute line — within the old ±5-line window of BOTH
        // bindings' method lines (|10-15|=5, |16-15|=1), but only the second binding's own
        // attribute/method line actually covers it.
        var result = RenameBindingResolver.FindBindingsAtCSharpMethod(registry, CsPath, line: 15);

        result.Should().ContainSingle();
        result[0].Expression.Should().Be("the second num is {int}");
    }

    [Fact]
    public void FindBindingsAtCSharpMethod_returns_all_attributes_stacked_on_the_same_method()
    {
        // [Given("alpha {int}")]   line 9
        // [Given("beta {int}")]    line 10
        // public void M(int x) { }  line 11
        var alpha = MakeBinding("alpha {int}", methodLine: 11, attributeSourceLine: 9);
        var beta = MakeBinding("beta {int}", methodLine: 11, attributeSourceLine: 10);
        var registry = ProjectBindingRegistry.FromBindings(new[] { alpha, beta });

        var result = RenameBindingResolver.FindBindingsAtCSharpMethod(registry, CsPath, line: 9);

        result.Should().HaveCount(2);
        result.Select(b => b.Expression).Should().BeEquivalentTo("alpha {int}", "beta {int}");
    }

    [Fact]
    public void FindBindingsAtCSharpMethod_returns_empty_when_no_binding_covers_the_line()
    {
        var first = MakeBinding("the first number is {int}", methodLine: 10, attributeSourceLine: 9);
        var registry = ProjectBindingRegistry.FromBindings(new[] { first });

        var result = RenameBindingResolver.FindBindingsAtCSharpMethod(registry, CsPath, line: 100);

        result.Should().BeEmpty();
    }
}
