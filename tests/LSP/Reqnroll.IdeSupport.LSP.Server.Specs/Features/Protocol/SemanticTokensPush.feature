Feature: Server-pushed semantic tokens for Visual Studio

Visual Studio's built-in LSP client cannot map Reqnroll's custom token types and does not reliably
pull semantic tokens, so for the Visual Studio client (--ide visualstudio) the server proactively
pushes encoded tokens via the custom reqnroll/semanticTokens notification. Every other client ignores
that notification and uses the standard pull-based flow, so the server must not push to them.

Scenario: The server pushes semantic tokens to the Visual Studio client
	Given the LSP server is started for IDE "visualstudio"
	When the feature file "push.feature" is opened with
		"""
		Feature: Pushed coloring

		Scenario: S
			When I press add
		"""
	Then the client receives a semantic tokens push for "push.feature"

Scenario: The server does not push semantic tokens to non-Visual-Studio clients
	Given the LSP server is started for IDE "vscode"
	When the feature file "nopush.feature" is opened with
		"""
		Feature: Pulled coloring

		Scenario: S
			When I press add
		"""
	Then the client receives no semantic tokens push
