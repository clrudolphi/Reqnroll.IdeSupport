namespace Reqnroll.IdeSupport.LSP.Core.Matching;

/// <summary>The Reqnroll hook attribute kind, identifying when in the test lifecycle a hook runs.</summary>
public enum HookType
{
    /// <summary>Hook type could not be determined.</summary>
    Unknown = 0,
    /// <summary>Runs once before any tests in the test run.</summary>
    BeforeTestRun = 1,
    /// <summary>Runs before each test thread.</summary>
    BeforeTestThread = 2,
    /// <summary>Runs before each feature.</summary>
    BeforeFeature = 3,
    /// <summary>Runs before each scenario.</summary>
    BeforeScenario = 4,
    /// <summary>Runs before each Given/When/Then block.</summary>
    BeforeScenarioBlock = 5,
    /// <summary>Runs before each step.</summary>
    BeforeStep = 6,
    /// <summary>Runs after each step.</summary>
    AfterStep = 7,
    /// <summary>Runs after each Given/When/Then block.</summary>
    AfterScenarioBlock = 8,
    /// <summary>Runs after each scenario.</summary>
    AfterScenario = 9,
    /// <summary>Runs after each feature.</summary>
    AfterFeature = 10,
    /// <summary>Runs after each test thread.</summary>
    AfterTestThread = 11,
    /// <summary>Runs once after all tests in the test run.</summary>
    AfterTestRun = 12,
}