# VS Code Extension — Implementation Plan

> **Status:** Phase 1 execution (T0–T2 complete)  
> **Branch:** `feat/vscode-extension-initial`  
> **Date:** 2026-06-29  
> **Source:** [Porting-to-VSCode-Rider-Analysis](Porting-to-VSCode-Rider-Analysis.md), [LSP-IDE-Support-Architecture](LSP-IDE-Support-Architecture.md)

---

## Completed (T0–T2)

| Task | Deliverable | Status |
|------|-------------|--------|
| **T0** | Extension project scaffolding (`src/VSCode/`) — `package.json`, `tsconfig.json`, `.vscodeignore`, ESLint, Prettier, TextMate grammar, `extension.ts` stub, Mocha test skeleton, `.vscode/launch.json`, `.vscode/tasks.json` | ✅ |
| **T1** | Multi-platform server publish — `scripts/publish-server.sh`, `scripts/build-vsix.sh`, multi-RID server path resolution in `extension.ts`, CI workflow (`.github/workflows/build-vscode-extension.yml`), Connector `RuntimeIdentifiers` + `.csproj` fixes | ✅ |
| **T2** | Test project scaffolding (`tests/VSCode/`) — standalone npm/Mocha project with compile + discover + execute verified | ✅ |

---

## Remaining Tasks

### Phase 2: Visual/Client-Side Features

---

#### T3 — TableHighlightService

**Scope:** Implement client-side per-cell text decorations for Gherkin data tables.

**Why client-side?** LSP semantic tokens cannot express per-pipe/per-cell granularity with correct alignment-relative colours. The only way to get correct table highlighting is a client-side decoration service.

**Deliverables:**
- `src/VSCode/src/tableHighlightService.ts` — class that:
  - Registers `vscode.TextEditorDecorationType` instances for light and dark themes
  - Listens to `vscode.window.onDidChangeVisibleTextEditors` and `vscode.workspace.onDidChangeTextDocument`
  - Parses the visible editor lines for Gherkin table rows (pipe-delimited)
  - Computes per-cell ranges and applies decorations
  - Updates decorations on edits and viewport changes
- Register the service in `extension.ts::activate()`
- Unit tests in `src/VSCode/src/test/` covering:
  - Cell range computation for simple and complex tables
  - Theme-aware colour selection
  - No-op for non-table lines
  - Cleanup on document close

**Effort:** ~200 lines TypeScript (service + tests)  
**Source:** PoC `tableHighlightService.ts` (~150 LOC) — adapt to Reqnroll's Gherkin parser output  
**Depends on:** T0 (scaffolding)  
**Verification:** `npm run compile && npm run lint`

---

#### T4 — Semantic Token Scopes and Formatter Defaults

**Scope:** Wire up the `package.json` contribution points that connect the LSP server's semantic token legend to VS Code's visual rendering and ensure Format Document routes to our server.

**Note:** The `semanticTokenScopes` and `configurationDefaults` entries are already stubbed in `package.json` (from T0). This task validates and refines them against the actual server legend.

**Deliverables:**
- `package.json` updates:
  - `contributes.semanticTokenScopes` — verify every `reqnroll.*` token type from the server legend has a corresponding VS Code TextMate scope
  - `contributes.configurationDefaults` — `[gherkin]`: `editor.defaultFormatter = "reqnroll.reqnroll-ide-support"`, `editor.formatOnType = true`
- Validation script or CI check that the `package.json` scopes match the server's legend output
- Update `src/VSCode/language-configuration.json` if indentation rules need refinement

**Effort:** ~30 lines in `package.json` + validation  
**Depends on:** T0  
**Verification:** Compile + lint; manual verification in VS Code Extension Dev Host that semantic tokens render correctly on a `.feature` file

---

#### T5 — TextMate Grammar Refinements

**Scope:** Improve the fallback TextMate grammar (`syntaxes/gherkin.tmLanguage.json`) created in T0 to cover more Gherkin constructs and reduce visual noise before the first LSP semantic token response.

**Deliverables:**
- `syntaxes/gherkin.tmLanguage.json` updates:
  - Language keywords: `Feature:`, `Rule:`, `Background:`, `Scenario:`, `Scenario Outline:`, `Scenario Template:`, `Examples:`, `Example:`
  - Step keywords: `Given`, `When`, `Then`, `And`, `But`, `*`
  - Tags: `@tag-name`
  - Comments: `# comment`
  - Strings: `"quoted"`, `"""docstring"""`
  - Placeholders: `<placeholder>`
  - Table delimiters: `|`
  - Numeric literals
  - Data table header separator rows
- Test files in `tests/VSCode/src/` covering grammar matching patterns

**Effort:** ~60 lines JSON  
**Depends on:** T0  
**Verification:** VS Code Extension Dev Host — open a `.feature` file and verify basic syntax colouring appears before the LSP server responds

---

### Phase 3: Project System Integration

---

#### T6 — Custom Notification Support (projectLoaded/projectFiles/projectUnloaded)

**Scope:** Implement the VS Code side of the `reqnroll/projectLoaded`, `reqnroll/projectFiles`, and `reqnroll/projectUnloaded` custom LSP notifications.

**Background:** The LSP server needs to know which assemblies are available for reflection-based binding discovery. In Visual Studio, the VS extension sends these notifications from the MSBuild project system. VS Code has no native MSBuild system, so the extension must either:
- (v1) Send best-effort folder-prefix project membership based on `.csproj`/`.slnx` files in the workspace
- (v2) Shell `dotnet msbuild` to evaluate `ProjectProperties` for each project

**Deliverables:**
- `src/VSCode/src/projectManager.ts` — class that:
  - Watches the workspace for `.slnx`, `.sln`, and `.csproj` files
  - On discovery, sends `reqnroll/projectLoaded` with folder-prefix membership (v1)
  - Sends `reqnroll/projectFiles` with the list of files under each project folder
  - Sends `reqnroll/projectUnloaded` when a project file is removed
  - Uses the `vscode-languageclient` `client.sendNotification(name, params)` API
- Registration in `extension.ts::activate()` — passes the `LanguageClient` reference to the manager
- Unit tests in `src/VSCode/src/test/` for:
  - Notification message construction
  - Project discovery from workspace folders
  - Folder-prefix file membership logic

**Effort:** ~150 lines TypeScript (v1 stub); optional +~100 lines for `dotnet msbuild` integration (v2)  
**Depends on:** T3 (LanguageClient reference available)  
**Risk:** R4 — VS Code has no first-class MSBuild project system. v1 accepts folder-prefix fallback, meaning linked files are not supported.  
**Verification:** Compile + lint; manual test with a workspace containing a `.csproj` and verify the LSP inspector log shows `reqnroll/projectLoaded` notifications

---

### Phase 4: Cross-Cutting & Polish

---

#### T7 — LSP Protocol-Level Spec Tests for VS Code Client Scenarios

**Scope:** Extend the existing Reqnroll spec test project (`tests/LSP/Reqnroll.IdeSupport.LSP.Server.Specs/`) with `.feature` scenarios that simulate VS Code's capability set.

**What this covers:** The LSP server is IDE-agnostic, but some behaviours vary based on the `--ide` flag. Spec tests with `--ide vscode` verify:
- Semantic tokens use pull mode (not push like VS)
- Standard LSP methods (`textDocument/definition`, `textDocument/completion`, etc.) return correct responses for VS Code's capabilities
- Custom notification handlers (`reqnroll/projectLoaded`, etc.) work correctly
- No VS-specific behaviours leak into the VS Code code path

**Deliverables:**
- New `.feature` file(s) in `tests/LSP/Reqnroll.IdeSupport.LSP.Server.Specs/`:
  - `VSCodeClientCapabilities.feature` — runs the server with `--ide vscode`, verifies pull-based semantic tokens, standard LSP routing
  - Optionally extend existing feature files with a VS Code client profile scenario outline
- Update spec test fixture to accept `--ide vscode` as a parameter

**Effort:** 2–3 `.feature` scenarios + fixture updates  
**Depends on:** T0, familiarity with existing Specs project structure  
**Verification:** `dotnet test tests/LSP/Reqnroll.IdeSupport.LSP.Server.Specs/` — all existing tests continue to pass, new scenarios pass

---

#### T8 — Architecture and Feature-Design Documentation

**Scope:** Update the existing architecture and feature-design documents with as-built VS Code extension implementation details.

**Deliverables:**
- `docs/LSP-IDE-Support-Architecture.md` §6.1 — add VS Code extension section covering:
  - Extension architecture (activation, LSP client, decoration services)
  - Server path resolution strategy
  - Multi-platform RID layout
  - Project notification approach (v1 folder-prefix)
  - Known limitations vs. Visual Studio client
- `docs/LSP-IDE-Support-Feature-Designs.md` — mark per-IDE support matrix for VS Code
- Document any VS Code-specific deviations from common LSP patterns

**Effort:** ~1–2 pages of markdown  
**Depends on:** T3–T6 (as-built details)  
**Verification:** Review for accuracy against actual implementation

---

### Phase 5: Build & CI Hardening

---

#### T9 — CI Reliability and Developer Workflow

**Scope:** Polish the build pipeline and developer experience.

**Deliverables:**
- Add `npm run format:check` to the CI `tsc-only` job
- Add `npm audit` or `npm outdated` checks (optional, non-blocking)
- Create `CONTRIBUTING.md` in `src/VSCode/` covering:
  - Prerequisites (Node.js, .NET SDK)
  - Development workflow (`npm run watch` + F5)
  - Server publish for development (`npm run build:server`)
  - Packaging (`npm run build:vsix`)
  - Running tests
- Add `.vscode/extensions.json` recommending the ESLint and Prettier VS Code extensions

**Effort:** ~1 hour  
**Depends on:** T1 (CI exists)  
**Verification:** CI passes on the branch

---

#### T10 — First-Run and User Experience

**Scope:** Implement basic user-facing features for the first experimental release.

**Deliverables:**
- Status bar indicator showing LSP server status (starting, running, stopped)
- Check for .NET runtime on first activation if using framework-dependent build
- Error notification if server binary is missing (with link to build instructions)
- Command palette entries for the commands already stubbed in `package.json`:
  - `reqnroll.defineSteps`
  - `reqnroll.goToStepDefinition`
  - `reqnroll.findStepUsages`
  - `reqnroll.renameStep`
  - `reqnroll.findUnusedStepDefinitions`
  - (These will be wired to actual LSP requests in a later iteration — for now, show an "LSP server not ready" message)

**Effort:** ~80 lines TypeScript  
**Depends on:** T3  
**Verification:** Extension Dev Host — verify status bar shows server state, commands appear in palette

---

#### T11 — End-to-End Validation and Stabilisation

**Scope:** Run the full CI pipeline end-to-end on a clean CI runner (or local equivalent), fix any issues, and prepare for first experimental release.

**Deliverables:**
- Successful CI run:
  - Server publish (all 4 RIDs)
  - Extension .vsix packaging
  - TypeScript compile + lint + format check
- Manual smoke test in VS Code Extension Dev Host:
  - Open a project with `.feature` files
  - Verify extension activates
  - Verify server starts (`--ide vscode`)
  - Verify basic syntax highlighting works (TextMate fallback + semantic tokens)
- Fix any issues found
- Tag a pre-release version (`0.1.0-experimental.1`)

**Effort:** 1–2 days  
**Depends on:** T3–T10  
**Verification:** CI green, manual smoke test passes

---

## Dependency Graph

```
T0 ─┬─→ T3 ──→ T4 ──→ T5 ──→ T7 ──→ T8
    │         │
    │         └──→ T6 ──────→ T8
    │
    ├─→ T1 ──→ T9
    │
    └─→ T2 (standalone test infra)
```

- **T3–T5** (visual features) are independent and can be done in any order after T0
- **T6** (project notifications) depends on T3 for the `LanguageClient` reference
- **T7** (spec tests) can start after T0 — it only extends existing .NET test projects
- **T8** (docs) is last — captures as-built details
- **T9–T11** are polish and validation — done after features stabilise

---

## Future Work (Phase 2 — Rider)

After VS Code Phase 1 is stable, the analysis recommends tackling Rider with these tasks:

| ID | Task | Effort |
|----|------|--------|
| T12 | Rider plugin scaffolding (Gradle, plugin.xml, FileType) | ~210 lines |
| T13 | Core LSP server bridge (LspServerSupportProvider, Descriptor) | ~55 lines |
| T14 | ImplicitReferenceProvider for cross-language navigation | ~150 lines |
| T15 | Semantic token TextAttributesKey mapping | ~50 lines |
| T16 | Custom notification transport | ~70 lines |
| T17 | Table cell decoration | ~200–400 lines |
| T18 | Gutter run icons | ~200–400 lines |
| T19 | Failing-step gutter marks | ~200–300 lines |

See the [Porting-to-VSCode-Rider-Analysis](Porting-to-VSCode-Rider-Analysis.md) §7.2 for the full Rider plan.

---

## Risk Register

| ID | Risk | Mitigation |
|----|------|------------|
| R4 | VS Code has no MSBuild project system — `projectLoaded` falls back to folder-prefix membership | Accept as v1 limitation; linked files not supported until `dotnet msbuild` eval is added |
| R5 | Maintaining three IDE client codebases simultaneously | LSP server is shared; glue layers are intentionally thin (~300 LOC for VS Code) |
| R8 | CI complexity with .NET + npm + vsce in one pipeline | Decoupled server publish and extension package CI jobs; `tsc-only` fast path skips server publish |
