#nullable enable

using Reqnroll.IdeSupport.LSP.Core.Editor.Services.Commenting;

namespace Reqnroll.IdeSupport.LSP.Core.Tests.Editor.Services.Commenting;

public class CommentToggleServiceTests
{
    private static CommentToggleService CreateSut() => new();

    // ── Toggle ON (comment uncommented lines) ─────────────────────────────

    [Fact]
    public void Single_uncommented_line_adds_hash_and_space()
    {
        var result = CreateSut().ToggleComment("Given a step\n", rangeStartLine: 0, rangeEndLine: 0);
        result.Edits.Should().ContainSingle()
            .Which.NewText.Should().Be("# Given a step");
    }

    [Fact]
    public void Multiple_uncommented_lines_all_get_hash_and_space()
    {
        var text = "Given step1\nWhen step2\nThen step3\n";
        var result = CreateSut().ToggleComment(text, 0, 2);
        result.Edits.Should().HaveCount(3);
        result.Edits[0].NewText.Should().Be("# Given step1");
        result.Edits[1].NewText.Should().Be("# When step2");
        result.Edits[2].NewText.Should().Be("# Then step3");
    }

    [Fact]
    public void Empty_line_gets_only_hash_without_space()
    {
        var result = CreateSut().ToggleComment("\n", 0, 0);
        result.Edits.Should().ContainSingle()
            .Which.NewText.Should().Be("#");
    }

    // ── Toggle OFF (uncomment all-commented lines) ────────────────────────

    [Fact]
    public void Single_commented_line_removes_hash_and_space()
    {
        var result = CreateSut().ToggleComment("# Given a step\n", 0, 0);
        result.Edits.Should().ContainSingle()
            .Which.NewText.Should().Be("Given a step");
    }

    [Fact]
    public void Single_hash_only_line_becomes_empty()
    {
        var result = CreateSut().ToggleComment("#\n", 0, 0);
        result.Edits.Should().ContainSingle()
            .Which.NewText.Should().Be("");
    }

    [Fact]
    public void All_commented_lines_all_get_uncommented()
    {
        var text = "# Given step1\n# When step2\n# Then step3\n";
        var result = CreateSut().ToggleComment(text, 0, 2);
        result.Edits.Should().HaveCount(3);
        result.Edits[0].NewText.Should().Be("Given step1");
        result.Edits[1].NewText.Should().Be("When step2");
        result.Edits[2].NewText.Should().Be("Then step3");
    }

    [Fact]
    public void Lines_with_leading_spaces_then_hash_are_uncommented()
    {
        var result = CreateSut().ToggleComment("    # indented comment\n", 0, 0);
        result.Edits.Should().ContainSingle()
            .Which.NewText.Should().Be("    indented comment");
    }

    // ── Mixed state (some commented, some not) → toggle all to comment ────

    [Fact]
    public void Mixed_commented_and_uncommented_lines_all_get_commented()
    {
        var text = "Given step1\n# When step2\nThen step3\n";
        var result = CreateSut().ToggleComment(text, 0, 2);
        result.Edits.Should().HaveCount(3);
        result.Edits[0].NewText.Should().Be("# Given step1");
        result.Edits[1].NewText.Should().Be("# # When step2");  // nested hash!
        result.Edits[2].NewText.Should().Be("# Then step3");
    }

    // ── Range in the middle of the document ───────────────────────────────

    [Fact]
    public void Only_lines_in_range_are_affected()
    {
        var text = "Feature: F\nGiven step1\nWhen step2\nThen step3\n";
        // comment only lines 1-2 (Given, When)
        var result = CreateSut().ToggleComment(text, rangeStartLine: 1, rangeEndLine: 2);
        result.Edits.Should().HaveCount(2);
        result.Edits[0].NewText.Should().Be("# Given step1");
        result.Edits[1].NewText.Should().Be("# When step2");
    }

    // ── Uncomment mixed → like mixed state, all turn into comment ─────────

    [Fact]
    public void Mixed_state_toggles_all_to_comment()
    {
        var text = "# Given step1\nWhen step2\n# Then step3\n";
        var result = CreateSut().ToggleComment(text, 0, 2);
        result.Edits.Should().HaveCount(3);
        result.Edits[0].NewText.Should().Be("# # Given step1");
        result.Edits[1].NewText.Should().Be("# When step2");
        result.Edits[2].NewText.Should().Be("# # Then step3");
    }
}
