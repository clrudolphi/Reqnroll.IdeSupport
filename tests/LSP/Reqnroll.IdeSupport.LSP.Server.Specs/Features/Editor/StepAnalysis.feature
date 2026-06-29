Feature: Step Analysis (F1)

The server annotates feature file steps via semantic tokens: defined steps carry no
error token; undefined steps receive a reqnroll.undefined_step token. Scope filtering
(tag, feature, scenario) is applied when the binding registry includes scoped bindings
(design doc F1).

This feature covers the token-level view of step state. Wire-level diagnostics
(publishDiagnostics for ambiguous/parse-error) are covered by unit tests.

Background:
    Given the LSP server is started

# ── Defined step: no undefined token ─────────────────────────────────────────────

Scenario: A defined step is not annotated as undefined
    When the project is announced with output assembly "Sample.dll" for "Defined.feature"
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
    And the feature file "Defined.feature" is opened with
        """
        Feature: Defined
        Scenario: S
            When I press add
        """
    Then the feature step "I press add" is reported as bound

# ── Undefined step: gets undefined token ──────────────────────────────────────────

Scenario: An undefined step is annotated as undefined
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
            When this step has no binding
        """
    Then the feature step "this step has no binding" is reported as unbound

# ── Tag-scoped bindings ─────────────────────────────────────────────────────────

Scenario: A tag-scoped step definition matches a tagged scenario but not an untagged one
    When the project is announced with output assembly "Sample.dll" for "Scoped.feature"
    And the C# step definition file "ScopedSteps.cs" is opened with
        """
        using Reqnroll;
        namespace Sample
        {
            [Binding]
            public class ScopedSteps
            {
                [When("I press multiply")]
                [Scope(Tag = "calculator")]
                public void WhenIPressMultiply() { }

                [When("I press add")]
                public void WhenIPressAdd() { }
            }
        }
        """
    And the feature file "Scoped.feature" is opened with
        """
        Feature: Scoped

        @calculator
        Scenario: Multiply
            When I press multiply

        Scenario: Add
            When I press add
        """
    Then the feature step "I press add" is reported as bound
    Then the feature step "I press multiply" is reported as bound

Scenario: A tag-scoped step definition does not match a scenario without the tag
    When the project is announced with output assembly "Sample.dll" for "Untagged.feature"
    And the C# step definition file "ScopedSteps.cs" is opened with
        """
        using Reqnroll;
        namespace Sample
        {
            [Binding]
            public class ScopedSteps
            {
                [When("I press multiply")]
                [Scope(Tag = "calculator")]
                public void WhenIPressMultiply() { }
            }
        }
        """
    And the feature file "Untagged.feature" is opened with
        """
        Feature: Untagged
        Scenario: S
            When I press multiply
        """
    Then the feature step "I press multiply" is reported as unbound

# ── Background steps ──────────────────────────────────────────────────────────

Scenario: Background steps are matched using the full scenario context
    When the project is announced with output assembly "Sample.dll" for "Background.feature"
    And the C# step definition file "Steps.cs" is opened with
        """
        using Reqnroll;
        namespace Sample
        {
            [Binding]
            public class Steps
            {
                [Given("the calculator is initialised")]
                public void GivenCalculatorIsInitialised() { }

                [When("I press add")]
                public void WhenIPressAdd() { }
            }
        }
        """
    And the feature file "Background.feature" is opened with
        """
        Feature: Background
        Background:
            Given the calculator is initialised

        Scenario: Add
            When I press add
        """
    Then the feature step "the calculator is initialised" is reported as bound
    Then the feature step "I press add" is reported as bound
