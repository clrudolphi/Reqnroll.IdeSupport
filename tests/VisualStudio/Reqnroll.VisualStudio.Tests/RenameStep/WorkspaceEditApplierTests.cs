using System;
using System.Collections.Generic;
using AwesomeAssertions;
using Reqnroll.IdeSupport.VisualStudio.Extension.RenameStep;
using Xunit;

namespace Reqnroll.VisualStudio.Tests.RenameStep;

/// <summary>
/// The pure text-splicing core of <c>WorkspaceEditApplier</c>
/// (<see cref="WorkspaceEditApplier.ApplyEditsToText"/>) — the half of the rename apply that
/// does not touch VS COM. Edits arrive bottom-to-top (as produced by
/// <see cref="RenameStepService.ParseWorkspaceEdit"/>), so offsets stay valid as edits apply.
/// </summary>
public class WorkspaceEditApplierTests
{
    private static string Nl(params string[] lines) => string.Join(Environment.NewLine, lines);

    [Fact]
    public void A_single_line_replacement_splices_the_new_text_into_the_line()
    {
        var text = "line0\nHELLO world\nline2";
        var edits = new List<TextEditItem> { new(1, 0, 1, 5, "Bye") };

        var result = WorkspaceEditApplier.ApplyEditsToText(text, edits);

        result.Should().Be(Nl("line0", "Bye world", "line2"));
    }

    [Fact]
    public void A_replacement_preserves_text_after_the_end_character()
    {
        // Rename the binding text inside a feature step, keeping the keyword prefix.
        var text = "Feature: F\nScenario: S\n    When I press add";
        var edits = new List<TextEditItem> { new(2, 9, 2, 20, "I choose add") };

        var result = WorkspaceEditApplier.ApplyEditsToText(text, edits);

        result.Should().Be(Nl("Feature: F", "Scenario: S", "    When I choose add"));
    }

    [Fact]
    public void A_multi_line_edit_collapses_the_spanned_lines_into_the_start_line()
    {
        var text = "a\nb\nc\nd";
        var edits = new List<TextEditItem> { new(1, 0, 2, 1, "X") };

        var result = WorkspaceEditApplier.ApplyEditsToText(text, edits);

        result.Should().Be(Nl("a", "X", "d"));
    }

    [Fact]
    public void Bottom_to_top_edits_on_the_same_line_keep_offsets_valid()
    {
        // Two edits on the same line, ordered bottom-to-top (descending start char).
        var text = "AAAA BBBB";
        var edits = new List<TextEditItem>
        {
            new(0, 5, 0, 9, "yyyy"), // later in the line first
            new(0, 0, 0, 4, "xxxx"),
        };

        var result = WorkspaceEditApplier.ApplyEditsToText(text, edits);

        result.Should().Be("xxxx yyyy");
    }

    [Fact]
    public void An_edit_starting_past_the_end_of_the_document_is_skipped()
    {
        var text = "only one line";
        var edits = new List<TextEditItem> { new(5, 0, 5, 1, "ignored") };

        var result = WorkspaceEditApplier.ApplyEditsToText(text, edits);

        result.Should().Be("only one line");
    }

    [Fact]
    public void Crlf_line_endings_are_handled()
    {
        var text = "line0\r\nHELLO\r\nline2";
        var edits = new List<TextEditItem> { new(1, 0, 1, 5, "Bye") };

        var result = WorkspaceEditApplier.ApplyEditsToText(text, edits);

        result.Should().Be(Nl("line0", "Bye", "line2"));
    }

    // ── ShouldNotifyDidChange: a closed .cs file rewritten by ApplyToDisk must be reported to
    //    the server too, or its Roslyn binding registry goes stale until the file is reopened. ──

    [Theory]
    [InlineData(@"C:\repo\Features\Calculator.feature", true)]
    [InlineData(@"C:\repo\StepDefinitions\CalculatorStepDefinitions.cs", true)]
    [InlineData(@"C:\repo\StepDefinitions\CalculatorStepDefinitions.CS", true)]
    [InlineData(@"C:\repo\reqnroll.json", false)]
    [InlineData(@"C:\repo\Notes.txt", false)]
    public void ShouldNotifyDidChange_covers_feature_and_cs_files_only(string path, bool expected)
    {
        WorkspaceEditApplier.ShouldNotifyDidChange(path).Should().Be(expected);
    }
}
