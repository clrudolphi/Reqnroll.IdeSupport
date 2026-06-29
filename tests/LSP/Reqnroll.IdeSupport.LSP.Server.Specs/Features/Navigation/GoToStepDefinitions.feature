Feature: Go to Step Definition (F5)

reqnroll/goToStepDefinitions on a cursor position in a .feature file returns
the step-definition binding locations with step type and method name (design doc F5).

The standard textDocument/definition handler is also available for generic LSP clients;
this custom request is used by the VS extension for its labelled picker.

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
            }
        }
        """
    And the feature file "Calculator.feature" is opened with
        """
        Feature: Calculator
        Scenario: Add
            Given the first number is 50
            When I press add
        """
    Then the feature step "the first number is 50" is reported as bound

# ── Defined step: location returned ────────────────────────────────────────────

Scenario: Returns the step definition location for a bound Given step
    # line 2 (0-based) = "    Given the first number is 50"
    When step definitions are requested at line 2 column 10 in "Calculator.feature"
    Then 1 step definition is returned
    And the step definitions include a location in "Steps.cs"

Scenario: Returns the step type and method name for a bound step
    # line 3 (0-based) = "    When I press add"
    When step definitions are requested at line 3 column 10 in "Calculator.feature"
    Then 1 step definition is returned
    And the step definitions include step type "When"
    And the step definitions include method name containing "WhenIPressAdd"

# ── Multiple matching bindings ─────────────────────────────────────────────────

Scenario: Returns all matching locations for a step matched by multiple bindings
    Given the LSP server is started
    When the project is announced with output assembly "Sample.dll" for "Outline.feature"
    And the C# step definition file "OutlineSteps.cs" is opened with
        """
        using Reqnroll;
        namespace Sample
        {
            [Binding]
            public class OutlineSteps
            {
                [When("I press (.*)")]
                public void WhenIPressAction(string action) { }

                [When("I press add")]
                public void WhenIPressAdd() { }
            }
        }
        """
    And the feature file "Outline.feature" is opened with
        """
        Feature: Outline
        Scenario Outline: actions
            When I press <action>
            Examples:
                | action |
                | add    |
        """
    When step definitions are requested at line 2 column 12 in "Outline.feature"
    Then 3 step definitions are returned

# ── Undefined step ─────────────────────────────────────────────────────────────

Scenario: Returns no step definitions for an undefined step
    Given the LSP server is started
    When the project is announced with output assembly "Sample.dll" for "Undef.feature"
    And the C# step definition file "EmptySteps.cs" is opened with
        """
        using Reqnroll;
        namespace Sample { [Binding] public class EmptySteps { } }
        """
    And the feature file "Undef.feature" is opened with
        """
        Feature: Undef
        Scenario: S
            Given a step that has no binding
        """
    When step definitions are requested at line 2 column 10 in "Undef.feature"
    Then 0 step definitions are returned

# ── Cursor not on a step ───────────────────────────────────────────────────────

Scenario: Returns no step definitions when the cursor is on the Feature header line
    When step definitions are requested at line 0 column 0 in "Calculator.feature"
    Then 0 step definitions are returned

# ── Non-feature file ───────────────────────────────────────────────────────────

Scenario: Returns no step definitions for a non-feature file
    Given the file "Notes.txt" is open with content
        """
        some notes
        """
    When step definitions are requested at line 0 column 0 in "Notes.txt"
    Then 0 step definitions are returned
