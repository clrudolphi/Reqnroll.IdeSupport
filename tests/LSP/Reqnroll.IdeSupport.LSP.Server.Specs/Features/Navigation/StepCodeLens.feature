Feature: Step Code Lens

Sending textDocument/codeLens for a C# step-binding file returns one CodeLens per
step-binding attribute, annotated with the number of matching feature-file steps
(design doc F18).

# Shared setup: announce the project, open a C# binding file with two step attributes
# on different methods, then open the feature file and wait for a step to be reported
# as bound (signals that binding discovery and the match cache are ready).
Background:
    Given the LSP server is started
    When the project is announced with output assembly "Sample.dll" for "Calculator.feature"
    And the C# step definition file "CalculatorSteps.cs" is opened with
        """
        using Reqnroll;
        namespace Sample
        {
            [Binding]
            public class CalculatorSteps
            {
                [Given("the first number is (.*)")]
                public void GivenTheFirstNumberIs(int number) { }

                [When("I add the numbers")]
                public void WhenIAddTheNumbers() { }
            }
        }
        """
    And the feature file "Calculator.feature" is opened with
        """
        Feature: Calculator
        Scenario: Add
            Given the first number is 50
            When I add the numbers
        """
    Then the feature step "the first number is 50" is reported as bound

Scenario: Code lens on a .cs binding file returns one lens per step-binding attribute
    When code lens is requested for "CalculatorSteps.cs"
    Then 2 code lenses are returned

Scenario: Lens title reflects the number of matching feature steps
    When code lens is requested for "CalculatorSteps.cs"
    Then the code lens at index 0 has title "1 step usage"

Scenario: Code lens uses reqnroll.findStepUsages command when there are usages
    When code lens is requested for "CalculatorSteps.cs"
    Then all code lenses have command "reqnroll.findStepUsages"

Scenario: Binding with no matching feature steps shows zero usages
    Given the LSP server is started
    When the project is announced with output assembly "NoMatch.dll" for "NoMatch.feature"
    And the C# step definition file "NoMatchSteps.cs" is opened with
        """
        using Reqnroll;
        namespace Sample
        {
            [Binding]
            public class NoMatchSteps
            {
                [Given("a step with no usages")]
                public void GivenNoUsages() { }
            }
        }
        """
    And the feature file "NoMatch.feature" is opened with
        """
        Feature: NoMatch
        Scenario: S
            Given an unrelated step
        """
    Then the feature step "an unrelated step" is reported as unbound
    When code lens is requested for "NoMatchSteps.cs"
    Then 1 code lens is returned
    And the code lens at index 0 has title "0 step usages"

Scenario: Code lens request on a .feature file returns no lenses
    When code lens is requested for "Calculator.feature"
    Then 0 code lenses are returned
