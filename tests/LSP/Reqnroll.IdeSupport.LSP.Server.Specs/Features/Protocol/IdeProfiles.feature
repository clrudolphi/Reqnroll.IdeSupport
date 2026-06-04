Feature: Shared semantic token legend across IDE identifiers

The --ide command-line argument is accepted for every supported client, but the semantic token
legend is now identical for all of them: every client receives the same custom reqnroll.* token
types and maps them to colours client-side.  These scenarios assert the server starts and serves
that shared legend for each known IDE identifier (and an unknown one), exercising the
--ide startup plumbing through the real startup path.

Scenario Outline: The server starts and advertises the shared custom legend for each IDE identifier
	Given the LSP server is started for IDE "<ide>"
	Then the server advertises a semantic tokens provider
	And the semantic tokens legend includes the token types
		| tokenType               |
		| reqnroll.keyword        |
		| reqnroll.step_parameter |
	When the feature file "ide.feature" is opened with
		"""
		Feature: Addition

		Scenario: Add
			When I press add
		"""
	Then the semantic tokens include a "reqnroll.keyword" token for "When"

Examples:
	| ide          |
	| visualstudio |
	| vscode       |
	| rider        |
	| unknown-ide  |
