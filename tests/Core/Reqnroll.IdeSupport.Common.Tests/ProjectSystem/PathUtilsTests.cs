using Reqnroll.IdeSupport.Common.ProjectSystem;

namespace Reqnroll.IdeSupport.Common.Tests.ProjectSystem;

public class PathUtilsTests
{
    // ── Regression: a sibling folder whose name extends the prefix must not match ──────────
    // Confirmed live: "C:\Repo\Minimalnet481\Foo.cs" is a plain string.StartsWith match for
    // "C:\Repo\Minimal", even though Minimalnet481 is a completely different project folder —
    // this let one project's step-definition bindings bleed into another's registry.

    [Fact]
    public void Sibling_folder_whose_name_extends_the_prefix_does_not_match()
    {
        PathUtils.IsUnderFolder(
                @"C:\Repo\Minimalnet481\StepDefinitions\Steps.cs",
                @"C:\Repo\Minimal")
            .Should().BeFalse();
    }

    [Fact]
    public void File_directly_under_the_folder_matches()
    {
        PathUtils.IsUnderFolder(
                @"C:\Repo\Minimal\StepDefinitions\Steps.cs",
                @"C:\Repo\Minimal")
            .Should().BeTrue();
    }

    [Fact]
    public void File_nested_several_levels_under_the_folder_matches()
    {
        PathUtils.IsUnderFolder(
                @"C:\Repo\Minimal\A\B\C\Steps.cs",
                @"C:\Repo\Minimal")
            .Should().BeTrue();
    }

    [Fact]
    public void Path_equal_to_the_folder_itself_matches()
    {
        PathUtils.IsUnderFolder(@"C:\Repo\Minimal", @"C:\Repo\Minimal")
            .Should().BeTrue();
    }

    [Fact]
    public void Trailing_separator_on_the_folder_is_tolerated()
    {
        PathUtils.IsUnderFolder(
                @"C:\Repo\Minimal\Steps.cs",
                @"C:\Repo\Minimal\")
            .Should().BeTrue();
    }

    [Fact]
    public void Comparison_is_case_insensitive()
    {
        PathUtils.IsUnderFolder(
                @"c:\repo\minimal\steps.cs",
                @"C:\Repo\Minimal")
            .Should().BeTrue();
    }

    [Fact]
    public void Unrelated_folder_does_not_match()
    {
        PathUtils.IsUnderFolder(
                @"C:\Repo\Other\Steps.cs",
                @"C:\Repo\Minimal")
            .Should().BeFalse();
    }

    [Theory]
    [InlineData(null, @"C:\Repo\Minimal")]
    [InlineData(@"C:\Repo\Minimal\Steps.cs", null)]
    [InlineData("", @"C:\Repo\Minimal")]
    [InlineData(@"C:\Repo\Minimal\Steps.cs", "")]
    public void Null_or_empty_inputs_do_not_match(string? filePath, string? folder)
    {
        PathUtils.IsUnderFolder(filePath, folder).Should().BeFalse();
    }
}
