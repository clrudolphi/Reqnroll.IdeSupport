Feature: Step Definition Sample Completion (F8)

textDocument/completion with the cursor after a Gherkin step keyword returns
step-definition sample completions derived from the binding registry (design doc F8).

Background:
    Given the LSP server is started
    When the project is announced with output assembly "Sample.dll" for "Calculator.feature"
    And the C# step definition file "Steps.cs" is opened with
        """
        using Reqnroll;
        namespace Sample
        {
            [Binding]
            public class Steps
            {
                [Given("the first number is (.*)")]
                public void GivenTheFirstNumberIs(int n) { }

                [When("I press add")]
                public void WhenIPressAdd() { }

                [Then("the result is (.*)")]
                public void ThenTheResultIs(int result) { }
            }
        }
        """
    And the feature file "Calculator.feature" is opened with
        """
        Feature: Calculator
        Scenario: Add
            Given the first number is 50
            When I press add
            Then the result is 60
        """
    Then the feature step "the first number is 50" is reported as bound

# ── Step completions returned after keyword ──────────────────────────────────

Scenario: Completion after Given keyword returns Given step samples
    # Line 2 = "    Given " (0-based); column 10 = just after "    Given "
    When completions are requested at line 2 column 10 in "Calculator.feature"
    Then completions are returned
    And the completions include a step label "the first number is [int]"

Scenario: Completion after When keyword returns When step samples
    # Line 3 = "    When "; column 9 = just after "    When "
    When completions are requested at line 3 column 9 in "Calculator.feature"
    Then completions are returned
    And the completions include a step label "I press add"

Scenario: Completion after Then keyword returns Then step samples
    # Line 4 = "    Then "; column 9 = just after "    Then "
    When completions are requested at line 4 column 9 in "Calculator.feature"
    Then completions are returned
    And the completions include a step label "the result is [int]"

# ── Wrong keyword block excluded ─────────────────────────────────────────────

Scenario: Given step completions do not include When or Then samples
    When completions are requested at line 2 column 10 in "Calculator.feature"
    Then completions are returned
    And the completions do not include a label "I press add"
    And the completions do not include a label "the result is [int]"
