using Reqnroll.IdeSupport.LSP.Core.Editor.Completions.Matching;

namespace Reqnroll.IdeSupport.LSP.Core.Tests.Editor.Completions.Matching;

public class ReturnAllCompletionMatcherTests
{
    private readonly ReturnAllCompletionMatcher _sut = new();

    [Fact]
    public void IsIncomplete_is_false()
        => _sut.IsIncomplete.Should().BeFalse();

    [Fact]
    public void Returns_all_candidates_in_stable_order()
    {
        var candidates = new[]
        {
            new StepCandidate("I have entered [int] into the calculator", 5),
            new StepCandidate("I press add",                              1),
            new StepCandidate("the result is [int]",                     3)
        };

        var result = _sut.Rank("", candidates);

        result.Select(r => r.Sample)
              .Should().ContainInOrder(
                  "I have entered [int] into the calculator",
                  "I press add",
                  "the result is [int]");
    }

    [Fact]
    public void Returns_all_candidates_regardless_of_typed_text()
    {
        var candidates = new[]
        {
            new StepCandidate("I have entered [int] into the calculator", 2),
            new StepCandidate("the result is [int]",                     1)
        };

        var result = _sut.Rank("xyz_no_match", candidates);

        result.Should().HaveCount(2);
    }

    [Fact]
    public void UsageCount_does_not_affect_ordering()
    {
        var candidates = new[]
        {
            new StepCandidate("alpha", 100),
            new StepCandidate("beta",    1)
        };

        var result = _sut.Rank("", candidates);

        result.Select(r => r.Sample).Should().ContainInOrder("alpha", "beta");
    }

    [Fact]
    public void Returns_empty_for_empty_input()
        => _sut.Rank("anything", Array.Empty<StepCandidate>()).Should().BeEmpty();
}
