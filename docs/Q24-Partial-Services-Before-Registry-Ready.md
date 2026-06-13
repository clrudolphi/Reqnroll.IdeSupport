# Q24 — Partial services when binding registry is not yet ready

**Status:** Plan drafted — not yet implemented
**Related:** [Q23 — Session restore LSP activation](Q23-Session-Restore-LSP-Activation.md)

---

## 1. Problem statement

When a `.feature` file is open but the binding registry has not yet been populated — either because
the out-of-proc Connector has not finished its first run, or because the file's project membership
has not yet been resolved — two handlers produce **misleading output** rather than neutral silence:

### 1a — Semantic tokens: steps coloured as *undefined*

The semantic token tagger emits step-status tokens (matched / unmatched / ambiguous) derived from
the binding match set. When the registry is `ProjectBindingRegistry.Invalid` or the project owner
is unknown, every step has no matching binding. The tagger therefore emits all steps as `unmatched`,
which the editor renders with the "undefined step" visual style (typically a distinct colour or
underline).

The user sees their steps highlighted as broken before the Connector has run even once. This is
indistinguishable from a genuine "step not implemented" error.

### 1b — Diagnostics: spurious *undefined step* warnings

`DiagnosticsPublishHandler` publishes binding-mismatch diagnostics (undefined / ambiguous / pending
steps) by reading the match set. When the registry is `Invalid`, the match set is empty, so all
steps appear undefined and the editor shows squiggle diagnostics on every step line.

Again, the user cannot distinguish "registry not loaded yet" from "step genuinely has no binding."

---

## 2. Scope of services affected

The audit below covers all LSP feature handlers and notes whether they already degrade correctly or
are affected by this issue.

### Features that already degrade correctly — no changes needed

| Feature | Behaviour with empty/unknown registry |
|---|---|
| Document Symbol (F9) | Reads only the parsed Gherkin AST — no registry access. Always returns full outline. |
| Formatting (F11/F12) | Purely syntactic. No registry access. |
| Keyword completions (F7) | Returns Gherkin dialect keyword list unconditionally. |
| Step completions (F8) | Returns empty list when registry is `Invalid`. No misleading data. |
| Parse-error diagnostics | Published from the buffer tag set regardless of registry state. |
| Go to Definition / Go to Step Definitions | Returns empty. Correct — cannot navigate to an unknown binding. |
| Go to Hooks | Returns empty. Same reason. |
| Find Step Usages (F14) | Returns `IsBinding=false`. No stale results. |
| Find Unused Step Definitions (F15) | Returns empty. Partial results would produce false "unused" positives. |
| Code Lens (`.cs` files) | Returns `[]` immediately when registry is `Invalid`. Correct — "0 usages" before the Connector runs would be wrong and alarming. |

### Features affected — changes required

| Feature | Current problem | Desired behaviour |
|---|---|---|
| **Semantic tokens** | Emits all steps as `unmatched` | Emit structural tokens only; omit step-status tokens |
| **Binding-mismatch diagnostics** | Publishes undefined/ambiguous/pending on every step | Suppress binding-mismatch diagnostics; publish parse errors only |

### `.cs` files — not applicable

The only LSP services provided for `.cs` files are code lens and find-step-usages, both of which
are fully binding-dependent. There is no syntactic layer to degrade to for `.cs` files. No changes
needed.

---

## 3. Definition of "registry not ready"

Both fixes use the same guard condition. The registry is **not ready** when either:

- `registry == ProjectBindingRegistry.Invalid` — the provider exists but the Connector has not
  completed its first successful run, OR
- Project ownership could not be resolved — `ResolveOwners(uri)` / `ResolvePrimaryOwner(uri)`
  returned an empty/unknown result, so no registry can even be retrieved.

The registry is **ready** (and binding-aware output is appropriate) once the Connector has completed
at least one successful run and replaced `ProjectBindingRegistry.Invalid` with a real registry.
This is signalled by `BindingRegistryChanged` with `isFullReplacement=true`.

---

## 4. Fix 1 — Semantic tokens: suppress step-status tokens when registry not ready

**Status: already implemented — no change required.**

`DeveroomTagParser.AddScenarioDefinitionBlockTag`
([`DeveroomTagParser.cs`](../src/LSP/Reqnroll.IdeSupport.LSP.Core/Editor/Services/Parsing/GherkinDocuments/DeveroomTagParser.cs))
already contains the guard:

```csharp
// Line 178-179 — fires after structural tags are added, before any binding-aware tag:
if (bindingRegistry == ProjectBindingRegistry.Invalid)
    continue;

// Line 228-229 — hook reference guard:
if (scenarioDefinition is Scenario scenario && bindingRegistry != ProjectBindingRegistry.Invalid)
```

When `bindingRegistry == ProjectBindingRegistry.Invalid`, the per-step loop emits only structural
tags (StepBlock, StepKeyword, DataTable, DocString, placeholder tags) and skips all binding-status
tags (DefinedStep, UndefinedStep, AmbiguousStep, BindingError, StepParameter).  The second guard
suppresses ScenarioHookReference tags for the same reason.

This is exactly the desired behaviour: structural tokens emitted, step-status tokens omitted when
the registry is not ready.  No code change is required.

---

## 5. Fix 2 — Diagnostics: suppress binding-mismatch entries when registry not ready

**File:** `DiagnosticsPublishHandler.cs`

### Change

Gate the loop that appends binding-mismatch diagnostics (undefined / ambiguous / pending step
entries) on a registry-ready check:

```csharp
// Before (simplified):
foreach (var mismatch in matchSet.Mismatches)
    diagnostics.Add(BuildMismatchDiagnostic(mismatch));

// After:
if (registry != ProjectBindingRegistry.Invalid)
{
    foreach (var mismatch in matchSet.Mismatches)
        diagnostics.Add(BuildMismatchDiagnostic(mismatch));
}
```

The `PublishDiagnosticsAsync` call at the end of the handler is unchanged — parse errors collected
earlier in the same method are always published.

### Expected result

Before the Connector has run: only genuine Gherkin syntax errors appear as diagnostics.
After the Connector has run: binding mismatches (undefined / ambiguous / pending steps) appear as
before.

### Verification

1. Open a solution cold.
2. Open a `.feature` file that has intentionally undefined steps AND a Gherkin syntax error.
3. Observe: only the syntax error is shown; the undefined-step squiggles are absent.
4. Wait for Connector to complete.
5. Observe: undefined-step squiggles appear for the intentionally undefined steps.

---

## 6. Interaction with session restore (Q23)

These fixes are complementary to the Q23 session-restore plan:

- Q23 ensures the LSP starts and the Connector begins as soon as possible on solution load.
- Q24 ensures that during the window between "LSP started" and "Connector finished", the UI shows
  neutral output rather than false errors.

Q24 is also valuable independent of Q23 — it applies any time the file is opened before binding
discovery has completed, including normal first-open scenarios on a slow machine or large solution.

---

## 7. Implementation notes

- Both fixes are **server-side only**. No VS extension changes required.
- Both fixes are **low risk**: they widen the conditions under which output is withheld, never the
  conditions under which incorrect output is shown.
- The two fixes should ship together: it is inconsistent to suppress undefined-step squiggles but
  still colour steps as unmatched (or vice versa).
- When the registry transitions from `Invalid` to ready, the existing `BindingRegistryChanged`
  notification already triggers a re-parse and re-publish for all open feature files. No additional
  refresh mechanism is needed — the correct output will appear automatically once the Connector
  finishes.
