#nullable enable

using System.Linq;
using Reqnroll.IdeSupport.LSP.Server.Benchmarks.Scenarios;

namespace Reqnroll.IdeSupport.LSP.Server.Tests.Performance;

public class SessionScheduleTests
{
    private static int SupersededCount(int bursts, double rate) =>
        Enumerable.Range(0, bursts).Count(i => SessionScenario.ShouldSupersede(i, rate));

    [Fact]
    public void Rate_zero_supersedes_nothing()
        => SupersededCount(100, 0.0).Should().Be(0);

    [Fact]
    public void Rate_one_supersedes_everything()
        => SupersededCount(100, 1.0).Should().Be(100);

    [Theory]
    [InlineData(0.25)]
    [InlineData(0.30)]
    [InlineData(0.50)]
    [InlineData(0.75)]
    public void Fractional_rate_supersedes_approximately_that_fraction(double rate)
    {
        const int bursts = 1000;
        var count = SupersededCount(bursts, rate);
        // Bresenham distribution lands within one burst of the exact fraction.
        count.Should().BeCloseTo((int)(bursts * rate), 1);
    }

    [Fact]
    public void Schedule_is_evenly_distributed_not_clustered()
    {
        // With rate 0.5, every other burst should supersede.
        var pattern = Enumerable.Range(0, 6).Select(i => SessionScenario.ShouldSupersede(i, 0.5)).ToArray();
        pattern.Should().Equal(false, true, false, true, false, true);
    }
}
