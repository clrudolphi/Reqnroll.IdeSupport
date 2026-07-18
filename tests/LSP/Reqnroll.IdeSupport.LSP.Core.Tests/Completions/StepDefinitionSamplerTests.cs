// #nullable disable — suppress nullable warnings; see issue #207
#nullable disable
using Reqnroll.IdeSupport.LSP.Core.Completions;

namespace Reqnroll.IdeSupport.LSP.Core.Tests.Completions;

public class StepDefinitionSamplerTests
{
    private static ProjectStepDefinitionBinding Binding(string regex, params string[] parameterTypes)
        => new(ScenarioBlock.Given,
               new Regex("^" + regex + "$"),
               null,
               new ProjectBindingImplementation("M1", parameterTypes, null));

    private readonly StepDefinitionSampler _sut = new();

    [Fact]
    public void Uses_regex_core_for_simple_stepdefs()
    {
        var result = _sut.GetStepDefinitionSample(Binding("I press add"));
        result.Should().Be("I press add");
    }

    [Theory]
    [InlineData("I have entered (.*) into the calculator",         "I have entered [int] into the calculator",    "System.Int32")]
    [InlineData("(.*) is entered into the calculator",             "[int] is entered into the calculator",         "System.Int32")]
    [InlineData("what I have entered into the calculator is (.*)", "what I have entered into the calculator is [int]", "System.Int32")]
    [InlineData("I have entered (.*) into the calculator",         "I have entered [string] into the calculator", "System.String")]
    [InlineData("I have entered (.*) into the calculator",         "I have entered [Version] into the calculator","System.Version")]
    [InlineData("I have entered (.*) and (.*) into the calculator","I have entered [int] and [???] into the calculator", "System.Int32")]
    [InlineData("I have entered (.*) and ([^\"]*) into the calculator",
                "I have entered [int] and [string] into the calculator", "System.Int32", "System.String")]
    public void Emits_param_placeholders(string regex, string expected, params string[] paramTypes)
    {
        var result = _sut.GetStepDefinitionSample(Binding(regex, paramTypes));
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(@"some \(context\)",       @"some (context)")]
    [InlineData(@"some \{context\}",       @"some {context}")]
    [InlineData(@"some \[context\]",       @"some [context]")]
    [InlineData(@"some \[context]",        @"some [context]")]
    [InlineData(@"some \[context] (.*)",   @"some [context] [???]")]
    [InlineData(@"chars \\\*\+\?\|\{\}\[\]\(\)\^\$\#", @"chars \*+?|{}[]()^$#")]
    public void Unescapes_masked_chars(string regex, string expected)
    {
        var result = _sut.GetStepDefinitionSample(Binding(regex));
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(@"foo (\d+) bar",               @"foo [int] bar")]
    [InlineData(@"foo (?<hello>.(.)) bar",       @"foo [int] bar")]
    [InlineData(@"foo (?<hello>.\)(.)) bar",     @"foo [int] bar")]
    public void Allows_nested_groups(string regex, string expected)
    {
        var result = _sut.GetStepDefinitionSample(Binding(regex, "System.Int32"));
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(@"foo? (\d+) bar")]
    [InlineData(@"foo (?:\d+) bar")]
    [InlineData(@"foo [a-z] bar")]
    [InlineData(@"foo. (\d+) bar")]
    [InlineData(@"foo* (\d+) bar")]
    [InlineData(@"foo+ (\d+) bar")]
    public void Falls_back_to_regex(string regex)
    {
        var result = _sut.GetStepDefinitionSample(Binding(regex, "System.Int32"));
        result.Should().Be(regex);
    }

    [Theory]
    [InlineData("(.*) is entered into the (very basic|standard|scientific) calculator",
                "[int] is entered into the (very basic|standard|scientific) calculator",
                "System.Int32", "System.String")]
    [InlineData("(.*) is entered into the ( 1st| 2nd | 3 rd |4th) calculator and saved with name ([^']*)",
                "[int] is entered into the ( 1st| 2nd | 3 rd |4th) calculator and saved with name [string]",
                "System.Int32", "System.String", "System.String")]
    public void Does_not_replace_choice_parameters(string regex, string expected, params string[] paramTypes)
    {
        var result = _sut.GetStepDefinitionSample(Binding(regex, paramTypes));
        result.Should().Be(expected);
    }
}
