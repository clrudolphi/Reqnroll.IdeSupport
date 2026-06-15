#nullable enable

using System.Text.RegularExpressions;
using Reqnroll.IdeSupport.LSP.Core.Rename;

namespace Reqnroll.IdeSupport.LSP.Core.Tests.Rename;

public class FeatureStepTextBuilderTests
{
    [Fact]
    public void Build_single_regex_param_replaces_group_with_captured_value()
    {
        var regex = new Regex("^the first number is (.*)$");
        var result = FeatureStepTextBuilder.Build(
            "the first no is (.*)", regex,
            "the first number is 50");

        result.Should().Be("the first no is 50");
    }

    [Fact]
    public void Build_multiple_regex_params_replaces_groups_in_order()
    {
        var regex = new Regex("^the (.*) is (\\d+)$");
        var result = FeatureStepTextBuilder.Build(
            "the (.*) was (\\d+)", regex,
            "the foo is 42");

        result.Should().Be("the foo was 42");
    }

    [Fact]
    public void Build_cucumber_expression_param_replaces_placeholder()
    {
        var regex = new Regex("^I have (\\d+) cukes$");
        var result = FeatureStepTextBuilder.Build(
            "I ate {int} cukes", regex,
            "I have 42 cukes");

        result.Should().Be("I ate 42 cukes");
    }

    [Fact]
    public void Build_no_parameters_returns_newName_as_is()
    {
        var regex = new Regex("^hello world$");
        var result = FeatureStepTextBuilder.Build(
            "hello there", regex,
            "hello world");

        result.Should().Be("hello there");
    }

    [Fact]
    public void Build_no_regex_match_returns_newName_as_is()
    {
        var regex = new Regex("^first (.*)$");
        var result = FeatureStepTextBuilder.Build(
            "new (.*)", regex,
            "does not match");

        result.Should().Be("new (.*)");
    }

    [Fact]
    public void Build_null_stepText_returns_newName_as_is()
    {
        var regex = new Regex("^hello (.*)$");
        var result = FeatureStepTextBuilder.Build(
            "hi (.*)", regex, null);

        result.Should().Be("hi (.*)");
    }

    [Fact]
    public void Build_null_regex_returns_newName_as_is()
    {
        var result = FeatureStepTextBuilder.Build(
            "hello (.*)", null,
            "hello world");

        result.Should().Be("hello (.*)");
    }

    [Fact]
    public void Build_skips_noncapturing_groups()
    {
        // (?:...) is not a capturing group — no captured value to replace.
        // The new expression is returned as-is since there are no parameters.
        var regex = new Regex("^I eat (?:a )?banana$");
        var result = FeatureStepTextBuilder.Build(
            "I ate (?:a )?banana", regex,
            "I eat a banana");

        result.Should().Be("I ate (?:a )?banana");
    }

    [Fact]
    public void Build_capturing_and_noncapturing_mixed()
    {
        // Only the capturing group (\d+) should be replaced; (?:green )? stays as-is.
        var regex = new Regex("^I ate (\\d+) (?:green )?bananas$");
        var result = FeatureStepTextBuilder.Build(
            "I consumed (\\d+) (?:green )?bananas", regex,
            "I ate 5 green bananas");

        result.Should().Be("I consumed 5 (?:green )?bananas");
    }

    [Fact]
    public void Build_cucumber_multi_param_replaces_all()
    {
        var regex = new Regex("^I have (\\d+) (\\w+)$");
        var result = FeatureStepTextBuilder.Build(
            "I ate {int} {string}", regex,
            "I have 42 apples");

        result.Should().Be("I ate 42 apples");
    }

    [Fact]
    public void Build_empty_stepText_returns_newName_as_is()
    {
        var regex = new Regex("^hello (.*)$");
        var result = FeatureStepTextBuilder.Build(
            "hi (.*)", regex, "");

        result.Should().Be("hi (.*)");
    }
}
