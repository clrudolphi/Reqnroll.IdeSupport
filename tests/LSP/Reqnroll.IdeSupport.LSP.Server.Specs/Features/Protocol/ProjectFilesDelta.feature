Feature: Project files delta (membership index removal)

The reqnroll/projectFiles notification (Q17 membership index) has a "delta" variant used by
IDE glue to report incremental file adds/removes without resending the whole project's file
list. Visual Studio drives file deletion exclusively through this delta path (via
IVsTrackProjectDocumentsEvents2) -- unlike VS Code, which additionally sends
workspace/didChangeWatchedFiles.

Regression coverage for issue #94: deleting a .cs step-definition file in Visual Studio left
its bindings live in the registry (step still shown as bound) because the delta path only
updated the membership index, never the registry itself.

Scenario: A bound step becomes unbound when its binding file is removed via a project files delta
	Given the LSP server is started
	When the project is announced with output assembly "Sample.dll" for "Calculator.feature"
	And the project files baseline is announced for "Sample.csproj" with
		| path              | role    |
		| Steps.cs          | Binding |
		| Calculator.feature | Feature |
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
	And the feature file "Calculator.feature" is opened with
		"""
		Feature: Calculator

		Scenario: Add
			When I press add
		"""
	Then the feature step "I press add" is reported as bound
	When the project files delta removes files for "Sample.csproj" with
		| path     | role    |
		| Steps.cs | Binding |
	Then the feature step "I press add" is reported as unbound

Scenario: Removing a feature file via a project files delta does not disturb other bindings
	Given the LSP server is started
	When the project is announced with output assembly "Sample.dll" for "Calculator.feature"
	And the project files baseline is announced for "Sample.csproj" with
		| path              | role    |
		| Steps.cs          | Binding |
		| Calculator.feature | Feature |
		| Extra.feature      | Feature |
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
	And the feature file "Calculator.feature" is opened with
		"""
		Feature: Calculator

		Scenario: Add
			When I press add
		"""
	Then the feature step "I press add" is reported as bound
	When the project files delta removes files for "Sample.csproj" with
		| path         | role    |
		| Extra.feature | Feature |
	Then the feature step "I press add" is reported as bound
