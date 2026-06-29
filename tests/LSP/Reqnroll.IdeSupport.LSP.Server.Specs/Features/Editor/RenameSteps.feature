Feature: Rename Steps (F16)

textDocument/rename from a cursor in a .cs step-definition file returns a WorkspaceEdit
that updates the attribute string literal and every matching step in feature files
(design doc F16).

The custom reqnroll/renameTargets request returns the list of renameable binding
expressions at the cursor; reqnroll/selectRenameTarget pre-selects one for the
subsequent rename when a method has multiple attributes.

# ── Simple rename from the C# side ────────────────────────────────────────────

Scenario: Renaming from the .cs file updates the attribute and all feature usages
    Given the LSP server is started
    When the project is announced with output assembly "Sample.dll" for "Calculator.feature"
    # Use "opened and saved to disk with" so FindAttributeLiteralAsync can read the file from disk.
    And the C# step definition file "Steps.cs" is opened and saved to disk with
        """
        using Reqnroll;
        namespace Sample
        {
            [Binding]
            public class Steps
            {
                [When("I press add")]
                public void WhenIPressAdd() { }
            }
        }
        """
    And the feature file "Calculator.feature" is opened with
        """
        Feature: Calculator
        Scenario: Add
            When I press add
        """
    Then the feature step "I press add" is reported as bound
    # Line 7 (0-based) is the method declaration "public void WhenIPressAdd() { }"
    When rename is requested at line 7 column 20 in "Steps.cs" with new name "I choose add"
    Then a workspace edit is returned
    And the workspace edit contains a change in "Steps.cs"
    And the workspace edit contains a change in "Calculator.feature"
    And the workspace edit changes to "Steps.cs" include new text "I choose add"
    And the workspace edit changes to "Calculator.feature" include new text "I choose add"

# ── Parameterized step rename ──────────────────────────────────────────────────

Scenario: Renaming a parameterized step preserves the parameter slots in feature usages
    Given the LSP server is started
    When the project is announced with output assembly "Sample.dll" for "Param.feature"
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
            }
        }
        """
    And the feature file "Param.feature" is opened with
        """
        Feature: Param
        Scenario: S
            Given the first number is 42
        """
    Then the feature step "the first number is 42" is reported as bound
    # Line 7 (0-based) is the method declaration "public void GivenTheFirstNumberIs(int n) { }"
    When rename is requested at line 7 column 20 in "Steps.cs" with new name "the operand is (.*)"
    Then a workspace edit is returned
    And the workspace edit contains a change in "Param.feature"
    And the workspace edit changes to "Param.feature" include new text "the operand is 42"

# ── prepareRename: no binding at cursor → no range returned ───────────────────

Scenario: Rename is not available when the cursor is not on a step binding
    Given the LSP server is started
    When the project is announced with output assembly "Sample.dll" for "NoRename.feature"
    And the C# step definition file "Steps.cs" is opened with
        """
        using Reqnroll;
        namespace Sample
        {
            [Binding]
            public class Steps
            {
                [When("I press add")]
                public void WhenIPressAdd() { }
            }
        }
        """
    And the feature file "NoRename.feature" is opened with
        """
        Feature: NoRename
        Scenario: S
            When I press add
        """
    Then the feature step "I press add" is reported as bound
    # Use prepareRename (not rename) to check availability without triggering an error response.
    # Line 0 (0-based) = "using Reqnroll;" — no step binding → prepareRename returns null.
    When prepare rename is requested at line 0 column 0 in "Steps.cs"
    Then no prepare rename range is returned

# ── Multi-attribute method: rename targets picker ─────────────────────────────

Scenario: A method with multiple step attributes returns one rename target per attribute
    Given the LSP server is started
    When the project is announced with output assembly "Sample.dll" for "Multi.feature"
    And the C# step definition file "Steps.cs" is opened with
        """
        using Reqnroll;
        namespace Sample
        {
            [Binding]
            public class Steps
            {
                [Given("I press add")]
                [When("I press add")]
                [When("I invoke add")]
                public void SharedMethod() { }
            }
        }
        """
    And the feature file "Multi.feature" is opened with
        """
        Feature: Multi
        Scenario: S
            When I press add
        """
    Then the feature step "I press add" is reported as bound
    # Line 9 (0-based) = "public void SharedMethod() { }"
    When rename targets are requested at line 9 column 20 in "Steps.cs"
    Then 3 rename targets are returned
    And the rename targets include expression "I press add"
    And the rename targets include expression "I invoke add"
