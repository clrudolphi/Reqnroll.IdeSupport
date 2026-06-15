#nullable enable

using System.Text.RegularExpressions;
using Reqnroll.IdeSupport.LSP.Core.Rename;

namespace Reqnroll.IdeSupport.LSP.Core.Tests.Rename;

public class FeatureStepTextBuilderTests
{
    [Fact]
    public void Build_single_param_replaces_value_preserving_concrete_value()
    {
        var result = FeatureStepTextBuilder.Build(
            "the first no is (.*)", "the first number is (.*)",
            new Regex("^the first number is (.*)$"),
            "the first number is 50");

        result.Should().Be("the first no is 50");
    }

    [Fact]
    public void Build_multiple_params_preserve_values_in_order()
    {
        var result = FeatureStepTextBuilder.Build(
            "the (.*) was (\\d+)", "the (.*) is (\\d+)",
            new Regex("^the (.*) is (\\d+)$"),
            "the foo is 42");

        result.Should().Be("the foo was 42");
    }

    [Fact]
    public void Build_cucumber_expression_param_preserves_value()
    {
        var result = FeatureStepTextBuilder.Build(
            "I ate {int} cukes", "I have {int} cukes",
            new Regex("^I have (\\d+) cukes$"),
            "I have 42 cukes");

        result.Should().Be("I ate 42 cukes");
    }

    [Fact]
    public void Build_no_parameters_returns_new_expression()
    {
        var result = FeatureStepTextBuilder.Build(
            "hello there", "hello world",
            new Regex("^hello world$"),
            "hello world");

        result.Should().Be("hello there");
    }

    [Fact]
    public void Build_static_text_does_not_align_returns_new_expression()
    {
        var result = FeatureStepTextBuilder.Build(
            "new (.*)", "first (.*)",
            new Regex("^first (.*)$"),
            "does not match");

        result.Should().Be("new (.*)");
    }

    [Fact]
    public void Build_null_stepText_returns_new_expression()
    {
        var result = FeatureStepTextBuilder.Build(
            "hi (.*)", "hello (.*)",
            new Regex("^hello (.*)$"), null);

        result.Should().Be("hi (.*)");
    }

    [Fact]
    public void Build_empty_stepText_returns_new_expression()
    {
        var result = FeatureStepTextBuilder.Build(
            "hi (.*)", "hello (.*)",
            new Regex("^hello (.*)$"), "");

        result.Should().Be("hi (.*)");
    }

    // ── Scenario Outline placeholder: the parameter value is "<secondNumber>", which does not
    //    match the binding's numeric regex. It must be preserved, not replaced by "{int}". ─────

    [Fact]
    public void Build_preserves_scenario_outline_placeholder()
    {
        var result = FeatureStepTextBuilder.Build(
            "the second no is {int}", "the second number is {int}",
            new Regex("^the second number is (-?\\d+)$"),
            "the second number is <secondNumber>");

        result.Should().Be("the second no is <secondNumber>");
    }

    [Fact]
    public void Build_preserves_quoted_string_argument_including_quotes()
    {
        // The {string} regex captures the inner text without quotes; static-segment substitution
        // keeps the quoted value verbatim.
        var result = FeatureStepTextBuilder.Build(
            "the two numbers {string} summed", "the two numbers {string} added",
            new Regex("^the two numbers (?:\"([^\"]*)\"|'([^']*)') added$"),
            "the two numbers 'are' added");

        result.Should().Be("the two numbers 'are' summed");
    }

    // ── Regex fallback: static text contains regex syntax (a non-capturing group) that does not
    //    appear verbatim in the step, so static-segment alignment fails and the regex path runs. ─

    [Fact]
    public void Build_falls_back_to_regex_when_static_text_is_regex_syntax()
    {
        var result = FeatureStepTextBuilder.Build(
            "I consumed (\\d+) (?:green )?bananas", "I ate (\\d+) (?:green )?bananas",
            new Regex("^I ate (\\d+) (?:green )?bananas$"),
            "I ate 5 green bananas");

        result.Should().Be("I consumed 5 (?:green )?bananas");
    }

    [Fact]
    public void Build_cucumber_multi_param_preserves_values()
    {
        var result = FeatureStepTextBuilder.Build(
            "I ate {int} {string}", "I have {int} {string}",
            new Regex("^I have (\\d+) (\\w+)$"),
            "I have 42 apples");

        result.Should().Be("I ate 42 apples");
    }

    [Fact]
    public void Build_no_oldExpression_falls_back_to_regex()
    {
        var result = FeatureStepTextBuilder.Build(
            "hi (.*)", oldExpression: null,
            new Regex("^hello (.*)$"),
            "hello world");

        result.Should().Be("hi world");
    }
}
