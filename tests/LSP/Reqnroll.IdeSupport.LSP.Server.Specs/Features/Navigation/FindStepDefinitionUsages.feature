Feature: Find Step Definition Usages

Sending textDocument/references from a cursor on a C# step binding method returns
Location entries pointing at every matching step in feature files — regardless of
whether those feature files are open in the editor (design doc F14).

Scenario: References for a bound step binding return the matching feature file location
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
		    }
		}
		"""
	And the feature file "Calculator.feature" is opened with
		"""
		Feature: Calculator
		Scenario: Add
		    Given the first number is 50
		"""
	Then the feature step "the first number is 50" is reported as bound
	When references are requested at line 7 column 0 in "CalculatorSteps.cs"
	Then 1 reference is returned
	And the references include a location in "Calculator.feature"

Scenario: No references are returned for a step binding with no matching steps in open files
	Given the LSP server is started
	When the project is announced with output assembly "Sample.dll" for "NoMatch.feature"
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
	When references are requested at line 7 column 0 in "CalculatorSteps.cs"
	Then 0 references are returned
