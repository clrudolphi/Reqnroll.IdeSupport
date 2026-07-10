namespace Reqnroll.IdeSupport.LSP.Core.Parsing.Gherkin;

/// <summary>The Given/When/Then section of a scenario that a step belongs to.</summary>
public enum ScenarioBlock
{
    /// <summary>The block could not be determined.</summary>
    Unknown = 0,
    /// <summary>The step is part of the Given (setup/preconditions) block.</summary>
    Given = 1,
    /// <summary>The step is part of the When (action) block.</summary>
    When = 2,
    /// <summary>The step is part of the Then (assertion) block.</summary>
    Then = 3
}
