# F16 · Step Rename Refactoring — Implementation Plan

> **Status:** Draft for review  
> **Audience:** Core team contributors  
> **Based on:** [Feature Designs §F16](LSP-IDE-Support-Feature-Designs.md) (revised)

---

## 1. Build Inventory

### 1.1 New components

| Component | Project | Type | Responsibility |
|-----------|---------|------|----------------|
| `StepRenameHandler` | `Reqnroll.IdeSupport.LSP.Server/Handlers/` | OmniSharp `IJsonRpcHandler` | Handles `textDocument/prepareRename` and `textDocument/rename`; delegates to `StepRenameValidator`, `StepDefinitionFileParser.GetAttributeStringInfo`, and custom request cycle |
| `StepRenameValidator` | `Reqnroll.IdeSupport.LSP.Core/Rename/` | Shared service | Applies Rules 1–8 (validation table in design doc); independently unit-testable |
| `RenameTargetsResponse` + `RenameTargetItem` | `Reqnroll.IdeSupport.LSP.Server/Protocol/` | Response DTOs | Response shape for `reqnroll/renameTargets`: `{ targets: [{ label, attributeRange }] }`. See existing `FindStepUsagesResponse` / `GoToHooksResponse` for the convention. |
| `RenameSessionManager` | `Reqnroll.IdeSupport.LSP.Core/Rename/` | Internal service | Tracks pending rename sessions (keyed by `(uri, version)` with 30-second expiry) for the multi-attribute picker flow |

### 1.2 Modified components

| Component | Project | Change |
|-----------|---------|--------|
| `StepDefinitionFileParser` | `LSP.Core/Discovery/` | New public method `GetAttributeStringInfo(CSharpStepDefinitionFile, int methodLine, int methodColumn, string expressionPattern) → AttributeStringInfo?` that reuses the existing attribute-walking logic (lines 197–242) to return `(TextSpan Span, SyntaxKind LiteralKind, string RawText)` — the source span, delimiter type, and escaped raw text of the attribute's string literal. |
| `ReqnrollCommandHandler` | `LSP.Server/Handlers/` | Add `reqnroll/renameTargets` and `reqnroll/selectRenameTarget` to the custom-command router (same pattern as `reqnroll/findStepUsages`) |
| `BindingMatchService` / match cache | `LSP.Core/Matching/` | Must expose a method to return all `.feature` step locations that match a given binding, **per project** — needed to build the WorkspaceEdit. (Currently only exposes per-file lookups; rename needs the inverse: "all files matching this binding.") |
| `LspWorkspaceScopeManager` | `LSP.Server/Workspace/` | May need `GetProjectsForUri(Uri)` to remain accessible from the rename handler for linked-file support (already exists for F14/F15 but verify it's accessible from the rename handler's scope) |
| `IMonitoringService` | `Reqnroll.IdeSupport.Common` | Uncomment `MonitorCommandRenameStepExecuted` and hook it into the rename handler's success path |
| `MonitoringService` | `VSSDKIntegration/Monitoring/` | Implement the uncommented method body (send `CommandRenameStep` telemetry) |

### 1.3 No-change components

| Component | Reason |
|-----------|--------|
| Binding Registry | No data model change needed — rename resolves attributes dynamically via the existing parser |
| `ConnectorBindingRegistryProvider` | No change — rename reads `.cs` via the existing parser, doesn't need connector metadata |
| Feature file document sync (`FeatureSyncHandler`) | No change — rename reads feature files via the match cache, not direct document access |
| VS `RenameStepCommand` (existing VSSDK) | Retained as-is for Phase 4 multi-attribute fallback; no changes until a later phase |

---

## 2. Build Plan (phased order)

### Phase A: Foundation — `StepDefinitionFileParser.GetAttributeStringInfo` + `StepRenameValidator` (estimated: 2–3 days)

These two components are independent of the LSP handler and can be built and tested in isolation.

**A1. `StepDefinitionFileParser.GetAttributeStringInfo`** (1 day)

- Target: `Reqnroll.IdeSupport.LSP.Core.Discovery.StepDefinitionFileParser` — new public method
- Input: `CSharpStepDefinitionFile`, method line/column from binding registry, binding expression string
- Output: `AttributeStringInfo?` — a new `sealed record` in the same file:
  ```csharp
  public sealed record AttributeStringInfo(
      Microsoft.CodeAnalysis.Text.TextSpan Span,     // exact source range of the string literal
      SyntaxKind LiteralKind,                        // StringLiteralToken vs SingleLineRawStringLiteralToken
      string RawText);                               // raw source text (escape sequences preserved)
  ```

Implementation (reuses existing code throughout):

```
1. Read .cs file content (StepDefinitionFileParser already receives CSharpStepDefinitionFile
   which wraps the document content — no new I/O plumbing needed)
2. Call Content.GetRootAsync() to get the SyntaxTree root (identical to ParseBindings line 79)
3. Walk DescendantNodes().OfType<MethodDeclarationSyntax>() and find the one whose
   GetSourceLocation() line/column matches the binding registry's method location
   (GetSourceLocation is already a private static method on this class — extract a
   line-match predicate from it)
4. Call EnumerateAttributes(method.AttributeLists) (already a private static — same line 168)
5. For each attribute, call GetStepDefinitionExpression(attribute) (already line 197)
   — if the expression matches the binding expression, proceed
6. Walk attribute.ArgumentList.Arguments to find the matching LiteralExpressionSyntax
7. Return AttributeStringInfo(argument.Token.Span, argument.Token.Kind(), argument.Token.Text)
8. If no literal found (constant ref, concatenation, no match) → return null
```

Total new code: ~30 lines. The method leans entirely on existing private helpers (`GetSourceLocation`, `EnumerateAttributes`, `GetStepDefinitionExpression`, `GetStringConstant`) — only the `AttributeStringInfo` record and the targeted method-lookup are new.

**Verification:** Unit test with ~10 `.cs` samples that specifically exercise the new targeted-lookup behaviour:
- Matching method at known line/column (single attribute)
- Matching method at known line/column (multiple attributes)
- Matching method when the expression is a verbatim string `@\"...\"`
- Matching method with escaped quotes in the literal
- Matching method with a derived attribute
- No matching attribute expression (simulated mismatch)
- Non-literal expression (constant reference `[Given(MyConst)]`) — must return `null`
- Method not found at the given line/column — must return `null`
- File-level (top-level) step definition
- Raw string literal (C# 11 `"""..."`)

---

**A2. `StepRenameValidator`** (1.5–2 days)

- Target: `Reqnroll.IdeSupport.LSP.Core/Rename/StepRenameValidator.cs`
- Methods:
  - `ValidateCursorPosition(Uri, Position) → (Binding?, ValidationError?)`  (Rule 1)
  - `ValidateExpressionIsStringLiteral(Binding) → ValidationError?` (Rule 2)
  - `ValidateNewName(Binding, string newName) → ValidationError?` (Rules 3–6)
  - `ValidateProjectState(ProjectScope) → ValidationError?` (Rule 7)

Key implementation details:
- Rules 3–6 reuse the existing `StepDefinitionExpressionParser` from `Reqnroll.IdeSupport.Common` (the same parser the VS `RenameStepCommand` uses)
- `ValidationError` is a record: `{ string Message, ValidationScope Scope }`
- The validator is **stateless** — all inputs are passed explicitly for testability

**Verification:** Unit test that covers every row of the validation table (Rules 1–8), plus:
- Boundary: empty string `""` as new name
- Boundary: new name identical to old name
- Boundary: parameter slot with no capture group (e.g., `(.*)(.*)` → `(.*)`)
- Scenario Outline: feature step text containing `<placeholder>` (Rule 6)
- Scenario Outline: feature step text without placeholders (allowed)
- Escaped operators in non-parameter parts: `\(`, `\)`, `\\` (must pass Rule 3)
- Unescaped operators: `?`, `*`, `+` in non-parameter parts (must fail Rule 3)
- Derived-attribute binding (must pass — attribute type is not the expression)

---

### Phase B: LSP Handler — single-binding rename (estimated: 3–4 days)

**B1. `StepRenameHandler`** — `textDocument/prepareRename` + `textDocument/rename` (2–3 days)

- Target: `Reqnroll.IdeSupport.LSP.Server/Handlers/StepRenameHandler.cs`
- Register as `IJsonRpcHandler` for both methods (same OmniSharp pattern as `FeatureDefinitionHandler`)
- Wire in `StepRenameValidator`, `StepDefinitionFileParser.GetAttributeStringInfo`, and the `BindingMatchService`

**`prepareRename` flow:**
1. Call `StepRenameValidator.ValidateCursorPosition(uri, position)`
2. Call `StepRenameValidator.ValidateExpressionIsStringLiteral(binding)`
3. Call `StepRenameValidator.ValidateProjectState(project)`
4. On any failure → return `null` (rename not available)
5. On success → return the range of the attribute string or step text

**`rename` flow:**
1. Resolve binding at `(uri, position)` via the Binding Match Service
2. Call `StepRenameValidator.ValidateNewName(binding, newName)` — if fails, return `ResponseError`
3. Get all feature step locations for this binding from the Binding Match Service (the inverse lookup: "all files matching this binding")
4. For each `.feature` step location, build a `TextEdit` replacing the step text with the new expression's plain-text rendering
5. If the cursor was on a C# file: compute `sameExprIndex` — the ordinal of this binding among same-expression bindings in the session-resolved list — and call `BuildCSharpEditAsync(uri, expression, newName, sameExprIndex)` to locate the correct method in the SyntaxTree by matching attribute string content (not PDB line numbers). When multiple methods share the same expression with different `[Scope]` attributes, the `sameExprIndex` picks the Nth matching method in document order.
6. Assemble `WorkspaceEdit` and return
7. On file-read errors: drop that file from the edit, include a `window/showMessage` warning
8. If the binding is linked (belongs to multiple projects): union the feature files from all including projects (via `GetProjectsForUri`)

**Verification:**
- Integration test (LSP protocol level) for `prepareRename`:
  - Cursor on step text in `.feature` → returns range
  - Cursor on attribute string in `.cs` → returns range
  - Cursor on method body (single attribute) → returns attribute range
  - Cursor on method body (multi-attribute) → returns `null`
  - Cursor on non-binding location → returns `null`
  - Uninitialized project → returns `null`
- Integration test for `rename`:
  - Rename from `.feature` step → all feature files + `.cs` attribute updated
  - Rename from `.cs` attribute → attribute string + all matching feature steps updated
  - Invalid new name (operators, parameter mismatch) → `ResponseError`
  - Scenario Outline step → `ResponseError`
  - Multiple feature files matching the same binding → all updated
- **Linked-file test** (requires the Q17 membership index to be active):
  - Binding linked into 2 projects → feature files in both projects included in WorkspaceEdit

---

**B2. Inverse lookup on BindingMatchService** (0.5–1 day)

The match cache currently stores "file → matches." Rename needs "binding → matching feature files." Two options:

**Option A** (preferred for Phase B): Add a method `GetMatchingStepLocations(Binding binding) → SourceLocation[]` that iterates the match cache's per-project index and collects all feature step locations that reference this binding.

**Option B** (simpler, slightly slower): Re-parse the match cache at rename time by iterating all open `.feature` files and checking which steps match the given binding. This avoids a data structure change but is O(feature file count) per rename.

**Decision:** Option A if the match cache already has a project-scoped step-to-binding mapping; otherwise Option B is acceptable for Phase B with a planned migration to Option A in Phase D.

---

### Phase C: Custom request flow — multi-attribute rename (estimated: 2–3 days)

**C1. `reqnroll/renameTargets` handler** (1 day)

- Add a new method on `StepRenameHandler` or a separate `RenameTargetsHandler`
- On request `(uri, position)`:
  1. Call `StepDefinitionFileParser.GetAttributeStringInfo` to enumerate all binding `AttributeArgumentSyntax` nodes on the method at the given line (reusing the same targeted-method-lookup logic from Phase A1)
  2. For each, extract the label (e.g., "Given I press add"). Do not DistinctBy(Expression) — multiple methods can share the same expression with different [Scope] attributes. Include scope-tag info in each label to disambiguate (e.g., "Given text [@tag1]" vs "Given text [@tag2]").
  3. Return RenameTarget[]
- If only one binding attribute exists, return a single-entry array (the picker still works but the client may skip the picker UI)

**C2. `RenameSessionManager`** (0.5 day)

- In-memory dictionary: `Dictionary<(Uri uri, int version), (Range attributeRange, DateTime expiresAt)>`
- On `reqnroll/selectRenameTarget { uri, version, attributeRange }`: create/update the session
- On `textDocument/rename`: check for a matching pending session; if found, use it to disambiguate the binding; if expired (>30s), ignore and fall back to single-binding resolution
- Background cleanup: on a timer or lazily on access, remove expired entries

**C3. `reqnroll/selectRenameTarget` handler** (0.5 day)

- Simple: validate `(uri, version)`, store the pending session, return `OK`
- If no matching method found at the given position, return `ResponseError`

**Verification:**
- Call `reqnroll/renameTargets` at a multi-attribute method → returns array with all attributes
- Call at a single-attribute method → returns array with one entry
- Call at a non-binding position → returns `ResponseError`
- Call `reqnroll/selectRenameTarget` → subsequent `textDocument/rename` uses the selected binding
- `textDocument/rename` without a prior `selectRenameTarget` on a multi-attribute method → `null` from `prepareRename` (handled by B1)
- Session expiry: wait 31 seconds (or mock the clock), verify `selectRenameTarget` is ignored

---

### Phase D: Client integration (estimated: 2–4 days per client)

**D1. Visual Studio** (2–3 days)

- **Single-binding path**: Modify `RenameStepCommand.PreExec` to detect single-binding cursor positions. When detected, route through the LSP `textDocument/rename` via `LspInterceptingPipe` (same pattern as F14's `FindStepUsagesCommand`). When not detected, use the existing `RenameStepViewModel` dialog + picker.
- **Multi-attribute path**: The existing VSSDK code handles this already; no changes needed. The custom dialog's expression validation is unchanged — it runs client-side via `StepDefinitionExpressionParser` as before.
- **Verification**: Run the existing `RenameStepCommandTests` (422 lines, 25+ test cases) to confirm no regression. Run new LSP integration tests for the delegated path.

**D2. VS Code** (1–2 days)

- **Single-binding path**: The standard LSP rename gesture (F2) works out of the box — no client code needed.
- **Multi-attribute fallback**: Add a keyboard shortcut in `package.json` for `reqnroll/renameStep` command. The command calls `reqnroll/renameTargets`, shows a `QuickPick` with the options, then calls `reqnroll/selectRenameTarget` and triggers `textDocument/rename`.
- **Verification**: Manual testing with the VS Code LSP client. Automated integration test via `vscode-languageclient` test harness if available.

**D3. Rider** (2–4 days — depends on PSI bridge maturity)

- **Single-binding path**: Standard LSP rename works for `.feature` files. For C# attribute renames, Rider's built-in resolve may intercept; test whether `textDocument/rename` is actually dispatched to the Reqnroll server or consumed by the C# PSI.
- **Multi-attribute fallback**: Implement an IntelliJ action (extends `AnAction`) that calls `reqnroll/renameTargets`, shows a `ListPopup`, and triggers the rename flow. Register the action in `plugin.xml`.
- **Verification**: Manual testing with Rider LSP client trace logs to confirm dispatch.

---

### Phase E: Polish (estimated: 2–3 days)

- **Telemetry**: Uncomment `MonitorCommandRenameStepExecuted` in `IMonitoringService` and `MonitoringService`. Fire `CommandRenameStep` on successful rename completion. Include `usageCount` (number of feature file edits) in the payload.
- **Error messages**: Review all `ResponseError` messages for clarity in the IDE rename dialog. The dialog is often small — messages should be short (<60 characters) and actionable.
- **Undo grouping**: Verify that the WorkspaceEdit's `TextEdit[]` per-file is applied as a single undo transaction in each IDE. For VS, this may require wrapping the pipe call in a `Microsoft.VisualStudio.Text.Operations.ITextUndoHistory` transaction.
- **Performance**: Profile `StepDefinitionFileParser.GetAttributeStringInfo` on a solution with 50+ step definition classes (worst case: user renames from step text, and every feature step location from the match cache must be resolved). Ensure the `prepareRename` response is <100ms and the `rename` response is <500ms for typical solutions.

---

## 3. Test Plan

### 3.1 Unit tests

| Test suite | Component | Count | Key scenarios |
|-----------|-----------|-------|---------------|
| `StepDefinitionFileParserTests` (new method `GetAttributeStringInfo`) | `StepDefinitionFileParser` | ~10 | Targeted method-lookup from known line/column; verbatim vs regular delimiters; raw text with escape sequences; non-literal expressions; method-not-found |
| `StepRenameValidatorTests` | `StepRenameValidator` | ~25 | Each validation rule (1–8) with passing + failing cases; boundary cases (empty string, identical rename, single parameter slot); all operator characters from Rule 3; parameter expression mismatches; Scenario Outline variants |
| `RenameSessionManagerTests` | `RenameSessionManager` | ~8 | Create session, retrieve session, expiry (30s), re-create on same key, session not found, multiple concurrent sessions |

### 3.2 LSP protocol integration tests

These test the actual LSP wire protocol through `StepRenameHandler` using a real OmniSharp server harness (the same pattern used by existing LSP feature tests).

| Test | Method(s) | Assertions |
|------|-----------|------------|
| `prepareRename_on_feature_step_returns_range` | `textDocument/prepareRename` | Returns `Range` containing the step text |
| `prepareRename_on_csharp_attribute_returns_range` | `textDocument/prepareRename` | Returns `Range` containing the attribute string (including delimiters) |
| `prepareRename_on_multi_attribute_method_returns_null` | `textDocument/prepareRename` | Returns `null` |
| `prepareRename_on_non_binding_position_returns_null` | `textDocument/prepareRename` | Returns `null` |
| `rename_simple_step_updates_both_files` | `textDocument/rename` | WorkspaceEdit contains exactly 2 document changes: `.cs` attribute + `.feature` step |
| `rename_with_parameters_preserves_slots` | `textDocument/rename` | `.feature` step text updated with parameter slots preserved |
| `rename_with_invalid_operators_returns_error` | `textDocument/rename` | ResponseError with "cannot contain expression operators" |
| `rename_with_parameter_count_mismatch_returns_error` | `textDocument/rename` | ResponseError with "Parameter count mismatch" |
| `rename_scenario_outline_step_returns_error` | `textDocument/rename` | ResponseError with "Could not rename step with placeholders" |
| `rename_with_escaped_operators_succeeds` | `textDocument/rename` | WorkspaceEdit with correct escaping in both files |
| `rename_from_step_text_single_binding` | `textDocument/rename` | Rename invoked from `.feature` line, `.cs` attribute also updated |
| `rename_scoped_duplicate_expression_all_targets_shown` | `reqnroll/renameTargets` | Array contains multiple entries for same expression with different scope tags in labels |
| `rename_scoped_duplicate_expression_csharp_edit_updates_correct_method` | `reqnroll/selectRenameTarget` + `textDocument/rename` | Only the selected method's attribute string is updated; other method with same expression unchanged |
| `rename_without_scope_tag_shows_plain_label` | `reqnroll/renameTargets` | Single-entry target label has no `[@...]` suffix |

### 3.3 Multi-attribute request flow tests (integration)

| Test | Method(s) | Assertions |
|------|-----------|------------|
| `renameTargets_on_multi_attribute_returns_all` | `reqnroll/renameTargets` | Array with correct labels + ranges |
| `renameTargets_on_single_attribute_returns_one` | `reqnroll/renameTargets` | Array with single entry |
| `renameTargets_on_non_binding_returns_error` | `reqnroll/renameTargets` | ResponseError |
| `selectRenameTarget_then_rename_uses_selected` | `reqnroll/selectRenameTarget` + `textDocument/rename` | Only the selected attribute is renamed |
| `selectRenameTarget_expired_then_rename_returns_error` | `reqnroll/selectRenameTarget` + 31s wait + `textDocument/rename` | ResponseError (session expired) |

### 3.4 Linked-file / cross-project tests

These require the Q17 membership index to be populated.

| Test | Setup | Assertions |
|------|-------|------------|
| `rename_from_linked_binding_updates_both_projects` | Binding `.cs` linked into Project A and Project B; each has a `.feature` using the step | WorkspaceEdit contains feature step edits from both projects |
| `rename_with_pending_membership_shows_warning` | Project B's `reqnroll/projectFiles` not yet received | WorkspaceEdit for Project A succeeds; `window/showMessage` warns about B |
| `rename_from_feature_step_updates_linked_binding` | Feature file in Project A uses linked binding from Project B | Feature steps in A + attribute in B's `.cs` file updated |

### 3.5 Existing VS regression tests

The following existing tests must continue to pass without modification (they test the VSSDK `RenameStepCommand`, which is retained as a façade):

| Test file | Test count | Notes |
|-----------|------------|-------|
| `RenameStepCommandTests.cs` | 25 (approx) | Covers expression validation, escaping, multi-attribute picker, Scenario Outline blocking, derived attributes |
| `RenameStepsCommand.feature` (SpecFlow) | 4 scenarios | BDD-level coverage of rename from code side, rename from feature file, multi-attribute picker, parameterized rename |

### 3.6 Manual testing checklist

| Scenario | To verify |
|----------|----------|
| Rename a parameterless step from `.feature` | Both files update, step text matches new expression |
| Rename a parameterized step from `.cs` attribute | Parameters preserved, both files update |
| Invoke rename on a multi-attribute C# method via F2 | F2 unavailable (`prepareRename` returns null) |
| Invoke rename on a multi-attribute method via custom command | Picker shows, selection works, rename applies to selected binding only |
| Rename a step used in 10+ `.feature` files | All 10 update in a single WorkspaceEdit |
| Rename a step with escaped parentheses `\(` | Escape sequences preserved in attribute string |
| Type `?` or `*` in a non-parameter part of the rename dialog | Error shown, rename blocked |
| Type different parameter count in rename dialog | Error shown, rename blocked |
| Rename a Scenario Outline step | Error shown, rename blocked |
| Open a `.feature` file in a linked project, rename the binding | Both the linking and the host project's feature files update |
| Modify the `.cs` file attribute string right before pressing F2 | Rename applies to current attribute text (not stale version) |

---

## 4. Dependency Graph

```
Phase A ──────────────────┐
  A1 GetAttributeStringInfo │
  A2 Validator               │
                             ▼
Phase B ───────────────────────────────┐
  B1 StepRenameHandler                  │
  B2 BindingMatchService inverse        │
                                        │
Phase C ───────────────────┐            │
  C1 renameTargets          │            │
  C2 SessionManager         │            │
  C3 selectTarget           │            │
                           ▼            ▼
Phase D ────────────────────────────────────────
  D1 Visual Studio  D2 VS Code  D3 Rider

Phase E (parallel with D, finishes after)
  Telemetry, error messages, undo, perf
```

- Phase A has **no** dependencies — start immediately.
- Phase B depends on Phase A (both components).
- Phase C depends on Phase A (GetAttributeStringInfo) and Phase B (StepRenameHandler as the host for the custom request methods). Can start in parallel with Phase B2.
- Phase D depends on Phase B (for single-binding path) and Phase C (for multi-attribute picker path). D1 (VS) has the lowest risk because the existing command is retained as fallback.
- Phase E runs in parallel with D but completes after.

---

## 5. Risk Table for Implementation

| # | Risk | Mitigation | Owner |
|---|------|------------|-------|
| I1 | `StepDefinitionFileParser.GetAttributeStringInfo` misidentifies the attribute when multiple attributes on the same method have identical expression strings (e.g., `[Given("X")]` x2) | The method matches by attribute position index, not just string content. The binding registry records which attribute index (0-based) produced the match, so `GetAttributeStringInfo` can target the correct one. Add attribute index to the method's lookup contract. |
| I2 | `BindingMatchService` inverse lookup (`binding → feature step locations`) is not implemented and Option B (re-parse at rename) is too slow for large solutions | Spike Option B first on a solution with 200+ `.feature` files. If >2s, implement Option A (pre-computed index) before shipping Phase B. |
| I3 | Rider's LSP client intercepts `textDocument/rename` for `.cs` files and never dispatches to the Reqnroll server | Verify with LSP trace logs in Phase D3. If confirmed, use the custom `reqnroll/renameStep` path exclusively for Rider's C# side (the `.feature` side still uses standard LSP). |
| I4 | WorkspaceEdit is too large for the LSP transport buffer (rename affecting 100+ feature files) | The LSP message size is bounded by the OmniSharp transport (stdio, no hard limit in spec). If impractical, split into batches or use `workspace/applyEdit` with `workspace/configuration` for confirmation. |
|| I5 | Vs-extensibility `ITextUndoHistory` is not accessible through the LSP pipe — WorkspaceEdit applied via `LspInterceptingPipe` does not merge into a single undo transaction | Accept as a known limitation in Phase 4; the user sees per-file undo entries (one per `.feature` file + `.cs` file). Improve in a later phase if users report it. |
|| I6 | Binding attribute argument is a constant reference (`[Given(MyStepConst)]`), not a string literal. The rename must be blocked because there is no literal to edit in the `.cs` file, and resolving the constant would require semantic analysis (a full `Compilation` with references — the same gap that limits F2's custom-derived-attribute discovery). | Block the rename with a clear error message: "Cannot rename: the step definition expression is not a string literal (e.g., it may be a constant)." Document as a known limitation alongside F2's custom-derived-attribute note. The existing `Constant_is_not_supported_in_step_definition_expression` test in `RenameStepCommandTests` already verifies this behaviour. |
|| I7 | Scoped duplicate expressions: multiple methods share the same expression but with different `[Scope]` tags. The rename must (a) show all variants in the picker with distinguishable labels, (b) resolve `textDocument/rename` to the correct method via `sameExprIndex`, and (c) update all feature steps regardless of which variant is selected (the scope is orthogonal to the expression text). | Picker labels include `[\@scopeTag]` suffix. `BuildCSharpEditAsync` indexes methods by `sameExprIndex` in SyntaxTree document order. Feature file edits are driven by the binding match service which already handles all scoped variants. |
