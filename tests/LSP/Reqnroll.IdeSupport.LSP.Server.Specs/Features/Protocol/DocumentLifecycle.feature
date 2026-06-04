Feature: Document lifecycle and refresh choreography

Multi-message scenarios that exercise the server-initiated workspace/semanticTokens/refresh
request and the didChange / didClose document sync flow over the wire.

Scenario: Editing a document triggers a server refresh request
	Given the LSP server is started
	When the feature file "edit.feature" is opened with
		"""
		Feature: Addition

		Scenario: Add
			When I press add
		"""
	And the feature file "edit.feature" is changed to
		"""
		Feature: Addition changed

		Scenario: Add
			Given I have entered 50 into the calculator
			When I press add
		"""
	Then the server requests a semantic tokens refresh

Scenario: Editing a document re-tokenizes the new content
	Given the LSP server is started
	When the feature file "retoken.feature" is opened with
		"""
		Feature: First
		"""
	And the feature file "retoken.feature" is changed to
		"""
		Feature: Second

		Scenario: Added later
			When I press add
		"""
	Then the semantic tokens include a "reqnroll.keyword" token for "Scenario:"

Scenario: A document can be closed and reopened with fresh content
	Given the LSP server is started
	When the feature file "closing.feature" is opened with
		"""
		Feature: Before close

		Scenario: Add
			When I press add
		"""
	And the feature file "closing.feature" is closed
	And the feature file "closing.feature" is opened with
		"""
		Feature: After reopen

		Scenario: Reopened
			Given I have entered 50 into the calculator
		"""
	Then the semantic tokens include a "reqnroll.keyword" token for "Given"

Scenario: Custom project notifications are accepted and editing still works
	Given the LSP server is started
	When the project is announced with output assembly "bin/Debug/Sample.dll" for "calc.feature"
	And the feature file "calc.feature" is opened with
		"""
		Feature: Addition

		Scenario: Add
			Given I have entered 50 into the calculator
		"""
	Then the semantic tokens include a "reqnroll.keyword" token for "Given"
	When the project is unloaded
	And the semantic tokens are requested again
	Then the semantic tokens include a "reqnroll.keyword" token for "Given"
