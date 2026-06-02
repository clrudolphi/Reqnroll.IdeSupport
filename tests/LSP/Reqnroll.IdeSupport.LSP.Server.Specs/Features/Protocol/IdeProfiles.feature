Feature: IDE-specific semantic token profiles

The --ide command-line argument selects the semantic token profile.  These scenarios assert the
server starts and serves a working legend for each known IDE identifier (and an unknown one),
exercising the SemanticTokenProfileFactory selection seam through the real startup path.

Scenario Outline: The server starts and advertises a legend for each IDE identifier
	Given the LSP server is started for IDE "<ide>"
	Then the server advertises a semantic tokens provider
	And the semantic tokens legend includes the token types
		| tokenType |
		| keyword   |
		| parameter |
	When the feature file "ide.feature" is opened with
		"""
		Feature: Addition

		Scenario: Add
			When I press add
		"""
	Then the semantic tokens include a "keyword" token for "When"

Examples:
	| ide          |
	| visualstudio |
	| vscode       |
	| rider        |
	| unknown-ide  |
