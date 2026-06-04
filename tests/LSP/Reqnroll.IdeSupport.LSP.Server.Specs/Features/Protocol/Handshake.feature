Feature: LSP protocol handshake and semantic tokens

The server is hosted in-process over an in-memory pipe and driven by a real LSP client,
so these scenarios exercise the actual initialize handshake and the textDocument/semanticTokens
request/response across the wire.

Scenario: Server completes the initialize handshake and advertises semantic tokens
	Given the LSP server is started
	Then the server advertises a semantic tokens provider
	And the semantic tokens legend includes the token types
		| tokenType                            |
		| reqnroll.keyword                     |
		| reqnroll.step_parameter              |
		| reqnroll.scenario_outline_placeholder |
		| reqnroll.undefined_step              |

Scenario: Keywords are tokenized over the wire for an opened feature file
	Given the LSP server is started
	When the feature file "calc.feature" is opened with
		"""
		Feature: Addition

		Scenario: Add two numbers
			Given I have entered 50 into the calculator
			When I press add
		"""
	Then the semantic tokens include a "reqnroll.keyword" token for "Feature:"
	And the semantic tokens include a "reqnroll.keyword" token for "Scenario:"
	And the semantic tokens include a "reqnroll.keyword" token for "Given"
	And the semantic tokens include a "reqnroll.keyword" token for "When"
