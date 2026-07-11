namespace Reqnroll.IdeSupport.LSP.Core.Parsing.Gherkin;

/// <summary>The Gherkin keyword a step was written with (independent of localized text).</summary>
public enum StepKeyword
{
    /// <summary>The step was written with a "Given" keyword.</summary>
    Given = 1,
    /// <summary>The step was written with a "When" keyword.</summary>
    When = 2,
    /// <summary>The step was written with a "Then" keyword.</summary>
    Then = 3,
    /// <summary>The step was written with an "And" keyword, continuing the previous block.</summary>
    And = 4,
    /// <summary>The step was written with a "But" keyword, continuing the previous block.</summary>
    But = 5
}
