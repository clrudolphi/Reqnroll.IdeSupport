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

# ── Per-IDE static capability advertisement ────────────────────────────────────
#
# textDocumentSync and renameProvider are gated on the client IDE identity because:
#   - vscode-languageclient v10 silently ignores dynamic client/registerCapability for
#     textDocument/didChange when the static textDocumentSync is absent from the
#     InitializeResult, so non-VS clients need the static entry.
#   - VS already handles dynamic-only textDocumentSync registration correctly, and
#     advertising renameProvider to VS would cause its standard rename UI to appear
#     alongside the custom reqnroll/renameTargets dialog.

Scenario Outline: Non-VS clients receive static textDocumentSync and renameProvider capabilities
	Given the LSP server is started for IDE "<ide>"
	Then the server statically advertises textDocumentSync with full sync and openClose
	And the server advertises renameProvider with prepareProvider

Examples:
	| ide     |
	| vscode  |
	| rider   |

# Note: a matching "VS client does not receive textDocumentSync" scenario is intentionally omitted.
# The in-process OmniSharp spec client merges dynamic client/registerCapability into ServerSettings,
# making static vs. dynamic textDocumentSync indistinguishable from the client side. The VS inspector
# logs confirm the static entry is absent in the real wire protocol. The behavioral coverage for VS
# textDocument/didChange is provided by the Handshake + DocumentLifecycle specs.

Scenario: VS client does not receive static renameProvider capability
	Given the LSP server is started for IDE "visualstudio"
	Then the server does not advertise renameProvider
