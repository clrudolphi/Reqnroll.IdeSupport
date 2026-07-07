# Rename Change Annotations — Implementation Plan

> **Status:** Phase 0 verified (2026-07-07) — go, VS on `Changes` fallback. Not yet implemented (Phases A–C).
> **Audience:** Core team contributors
> **Based on:** [F16 Step Rename](F16-Step-Rename-Implementation-Plan.md); LSP 3.16 `ChangeAnnotation` / `AnnotatedTextEdit`
> **Library support:** Already modelled in OmniSharp.Extensions.LanguageServer 0.19.9
> (`ChangeAnnotation`, `AnnotatedTextEdit`, `WorkspaceEdit.DocumentChanges`, `WorkspaceEdit.ChangeAnnotations`) — no custom DTO plumbing.

---

## 1. Nature of the changes

Today [`StepRenameHandler.HandleRenameAsync`](../src/LSP/Reqnroll.IdeSupport.LSP.Server/Features/Rename/StepRenameHandler.cs) returns a `WorkspaceEdit` built from the **`Changes`** map (`Dictionary<DocumentUri, IEnumerable<TextEdit>>`). The client applies every edit silently and atomically. For a step rename this can touch many `.feature` files plus the binding `.cs` — the user gets no per-file preview, no grouping, and no opportunity to opt a file out.

LSP 3.16 added **change annotations**: each edit can carry an annotation id; the workspace edit declares a dictionary of `ChangeAnnotation { Label, NeedsConfirmation, Description }`. Compliant clients render a grouped, reviewable preview keyed by annotation and honour `needsConfirmation` (the user confirms before the edit applies).

The change is mechanical and additive:

1. Switch the rename result from `WorkspaceEdit.Changes` to `WorkspaceEdit.DocumentChanges` (required — annotations attach only to `AnnotatedTextEdit`, which lives in `TextDocumentEdit`, not in the plain `Changes` map).
2. Tag edits with one of two annotation ids: **`reqnroll.rename.feature`** (feature-file step text) and **`reqnroll.rename.binding`** (the `.cs` attribute literal).
3. Negotiate the client capability and **fall back to the existing `Changes` shape** when the client does not advertise annotation support.

No matching/resolution logic changes — only the shape of the returned edit.

---

## 2. Structural changes to DTOs

All types below already exist in the OmniSharp protocol assembly; this is a re-shape of the value we return, not a new contract.

| Concern | Today | After |
|---|---|---|
| Edit container | `WorkspaceEdit.Changes : IDictionary<DocumentUri, IEnumerable<TextEdit>>` | `WorkspaceEdit.DocumentChanges : Container<WorkspaceEditDocumentChange>` (each a `TextDocumentEdit`) |
| Per-file identity | `DocumentUri` key | `OptionalVersionedTextDocumentIdentifier { Uri, Version }` (version null where the doc is closed) |
| Edit type | `TextEdit` | `TextEditOrAnnotatedTextEdit` — wraps `AnnotatedTextEdit { Range, NewText, AnnotationId }` |
| Annotation catalogue | — (none) | `WorkspaceEdit.ChangeAnnotations : IDictionary<ChangeAnnotationIdentifier, ChangeAnnotation>` |

**New constants** (add to a small static holder, e.g. `RenameChangeAnnotations` beside the handler):

```csharp
internal static class RenameChangeAnnotations
{
    public const string Feature = "reqnroll.rename.feature";
    public const string Binding = "reqnroll.rename.binding";
}
```

**Capability gate** — read once from `ClientCapabilities` (already reachable in the handler-registration path via `IClientCapabilityProvider`/`ILanguageServerFacade.ClientSettings`):

- `clientCapabilities.Workspace.WorkspaceEdit.DocumentChanges == true`
- `clientCapabilities.Workspace.WorkspaceEdit.ChangeAnnotationSupport != null`

When either is false, return the existing `Changes`-shaped edit unchanged.

---

## 3. New classes and methods

Deliberately small — the work concentrates in the existing handler.

| Component | Project / file | Type | Responsibility |
|---|---|---|---|
| `RenameChangeAnnotations` | `LSP.Server/Features/Rename/` | static class | The two annotation-id constants + the two `ChangeAnnotation` factory descriptors. |
| `WorkspaceEditBuilder` | `LSP.Server/Features/Rename/` | internal class | Accumulates `(DocumentUri, version, AnnotatedTextEdit)` tuples and emits **either** a `DocumentChanges`-shaped edit (annotations on) **or** a `Changes`-shaped edit (fallback). Centralises the branch so the handler stays readable. |
| `IClientCapabilityProvider.SupportsChangeAnnotations` | `LSP.Server/Workspace/` (extend existing client-context accessor) | property | Cached negotiation result, set during `OnStarted`. |

**`StepRenameHandler` changes** — replace the `changes` dictionary accumulation (lines ~282–354) with `WorkspaceEditBuilder`:

```csharp
var builder = new WorkspaceEditBuilder(_clientCapabilities.SupportsChangeAnnotations);
builder.DeclareAnnotation(RenameChangeAnnotations.Feature,
    new ChangeAnnotation { Label = $"Rename step usages → \"{newName}\"", NeedsConfirmation = false });
builder.DeclareAnnotation(RenameChangeAnnotations.Binding,
    new ChangeAnnotation { Label = "Update step-definition attribute", NeedsConfirmation = false });

// feature edits
builder.Add(featureUri, documentVersion: null, RenameChangeAnnotations.Feature, range, featureNewText);
// cs edit
builder.Add(csUri, documentVersion: null, RenameChangeAnnotations.Binding, csEdit.Range, csEdit.NewText);

return builder.Build();   // DocumentChanges-shaped when supported, else Changes-shaped
```

`NeedsConfirmation` is left `false` by default (rename is already user-initiated and validated). It is exposed on the builder so a later setting — e.g. "confirm cross-project renames" — can flip the feature annotation to `true` when `ResolveOwners(uri).Count > 1`.

The match-cache invalidation pass (lines ~335–343) is unchanged and runs against `builder.TouchedFeatureUris`.

---

## 4. Surfacing in the Visual Studio client

**No new interceptor or VSSDK command is required on the happy path.** Rename already flows through the standard `textDocument/rename` round trip; VS applies whatever `WorkspaceEdit` shape the server returns. The work is capability negotiation and a verification gate, consistent with [[feedback-vs-specific-gating]].

Key points and risks specific to the VS.Extensibility client ([`ReqnrollLanguageClient`](../src/VisualStudio/Reqnroll.IdeSupport.VisualStudio.Extension/ReqnrollLanguageClient.cs)):

1. **`DocumentChanges` must be honoured.** The VS LSP client must advertise `workspace.workspaceEdit.documentChanges` and `changeAnnotationSupport` in its `initialize` capabilities. **This must be verified against the actual `initialize` payload** — capture it from the existing `LspInspectorLogger` session log (`%LocalAppData%\Reqnroll\reqnroll-vs-inspector-*.log`). If VS does not advertise `changeAnnotationSupport`, our negotiation gate makes the server emit the legacy `Changes` shape automatically, so rename keeps working with no preview.
2. **`needsConfirmation` preview UI is client-rendered.** Even where VS accepts annotated edits, whether it surfaces the grouped-confirmation preview is a VS client behaviour we do not control. Treat the confirmation UX as best-effort; correctness of the edit must not depend on it. Default `NeedsConfirmation = false` avoids relying on a preview that may not appear.
3. **The custom multi-attribute path is unaffected.** `reqnroll/renameTargets` / `reqnroll/selectRenameTarget` (handled by `RenameStepService` over the intercepting pipe) feed the *selection* before `textDocument/rename` runs; they do not carry edits and need no change.
4. **`OptionalVersionedTextDocumentIdentifier.Version = null`** for closed feature files. VS must accept versionless document edits (it does for `Changes` today); confirm the same holds for `DocumentChanges` during verification.

If verification shows VS silently drops annotated edits (rather than applying them un-annotated), the fallback gate should be made **VS-specific** (`ClientIdeContext.IsVisualStudio`) so VS Code / Rider still get the richer shape while VS stays on `Changes`.

---

## 5. Impact on testing

### 5.1 Approach

- **Unit (server)** — extend `StepRenameHandlerTests` and add `WorkspaceEditBuilderTests`. Drive `HandleRenameAsync` with a stubbed `IClientCapabilityProvider` in both modes and assert on the returned `WorkspaceEdit` shape. Build inputs directly per [[core-tests-avoid-stubidescope]].
- **Acceptance (specs)** — extend the F16 `.feature` specs run through `LspServerHarness`, negotiating change-annotation support in the simulated `initialize`.
- **VS manual verification** — one pass in the experimental instance against a multi-project solution, reading the inspector log to confirm the negotiated capability and the applied edit.

### 5.2 Test conditions

| # | Condition | Expected |
|---|---|---|
| 1 | Client advertises annotation support; single-project rename | `DocumentChanges` returned; every edit is `AnnotatedTextEdit`; `ChangeAnnotations` has `feature` + `binding`; `Changes` null |
| 2 | Client does **not** advertise support | Legacy `Changes` map returned; no `DocumentChanges`, no `ChangeAnnotations` (byte-identical to today) |
| 3 | Feature-only rename (no `.cs` edit resolved) | Only the `feature` annotation declared; `binding` annotation absent |
| 4 | Cross-project rename, ≥2 owners | (When the "confirm cross-project" toggle is on) `feature` annotation `NeedsConfirmation = true` |
| 5 | Closed feature file among targets | Its `TextDocumentEdit` carries `Version = null`; edit still present |
| 6 | Annotation ids referenced by edits all exist as keys in `ChangeAnnotations` | No dangling `AnnotationId` (protocol-validity invariant) |
| 7 | Match-cache invalidation still fires for every touched `.feature` URI | Parity with current behaviour |
| 8 | `prepareRename` unchanged | Existing F16 prepareRename tests stay green |

**Regression guard:** keep one explicit assertion that mode-2 output is structurally identical to the pre-change `WorkspaceEdit`, so the fallback can never silently diverge.

---

## 6. Phased build plan & effort

### Phase 0 — Capability verification (go/no-go) · ~0.5 day — **DONE (2026-07-07)**

The whole feature hinges on the VS LSP client advertising annotation support; resolve this **before** writing code.

- Launch the experimental VS instance against a multi-project solution with `.feature` files; trigger any rename.
- From the inspector log (`%LocalAppData%\Reqnroll\reqnroll-vs-inspector-*.log`) capture the client `initialize` capabilities and check:
  - `capabilities.workspace.workspaceEdit.documentChanges == true`
  - `capabilities.workspace.workspaceEdit.changeAnnotationSupport != null`
- **Decision rule:**
  - Both present → proceed at full scope (annotated `DocumentChanges` for all clients).
  - Present in VS Code/Rider, absent in VS → proceed; VS runs the `Changes` fallback. Record as a known limitation in §7.
  - Absent everywhere → shelve; no client benefit to building it.

**Observed results (captured from live `initialize` payloads, 2026-07-07):**

| Client | `documentChanges` | `changeAnnotationSupport` |
|---|---|---|
| Visual Studio | `true` | absent |
| VS Code 1.127.0 | `true` | `{ "groupsOnLabel": true }` |
| Rider | not yet captured | not yet captured |

**Decision: GO**, per the "present in VS Code/Rider, absent in VS" rule — proceed with the full feature; VS negotiates down to the `Changes` fallback and sees no regression. Rider capture still outstanding but does not block the decision (VS Code alone already justifies building it).

### Phase A — `WorkspaceEditBuilder` + negotiation (~1 day)
Standalone, fully unit-testable. Build the dual-mode builder and the `SupportsChangeAnnotations` accessor wired in `OnStarted`. No handler change yet.

### Phase B — Handler reshape + fallback (~1 day)
Re-point `HandleRenameAsync` at the builder; keep the match-cache invalidation pass. Land behind the negotiation gate so the default-off path is byte-identical to today.

### Phase C — Tests + VS verification (~1 day)
Unit + spec conditions (§5), then the experimental-instance pass confirming applied edits and undo behaviour (§9).

**Total: ~3.5 days** (0.5 of which is verification, not code).

---

## 7. Cross-client support matrix

| Client | Negotiated mode | User-visible result |
|---|---|---|
| VS Code | `DocumentChanges` + annotations | Grouped, labelled rename preview |
| Rider | `DocumentChanges` + annotations | Grouped, labelled rename preview (JetBrains LSP) |
| Visual Studio | TBD by Phase 0 — likely `Changes` fallback | Edits applied silently as today; no regression |

The matrix is the contract for release notes: only clients in the first two rows gain new UX; VS is "no worse than today" until verified otherwise.

---

## 8. Telemetry

Extend the existing rename event (already emitted at [`StepRenameHandler`](../src/LSP/Reqnroll.IdeSupport.LSP.Server/Features/Rename/StepRenameHandler.cs) success path) rather than adding a new one:

| Property | Type | Purpose |
|---|---|---|
| `ChangeAnnotationsUsed` | bool | Did this rename emit the annotated `DocumentChanges` shape or the fallback? Lets us measure real-world client support in the field instead of per-session log reading. |
| `OwnerCount` | int | Number of owning projects — feeds the cross-project-confirmation decision (§3). |
| `EditedFileCount` | int | Blast radius of the rename. |

No new telemetry plumbing — these are added dimensions on the existing event.

---

## 9. Risks & open questions

| # | Item | Disposition |
|---|---|---|
| RA-1 | **Undo granularity** — does VS apply an annotated multi-file `DocumentChanges` edit as a *single* undo transaction, or one-per-file? A multi-undo would surprise users. | Verify in Phase C; if VS splits, note it — it is a client behaviour we cannot reshape. |
| RA-2 | VS accepts annotated edits but ignores `needsConfirmation` preview | Acceptable — default `NeedsConfirmation = false`; correctness never depends on the preview. |
| RA-3 | Versionless `OptionalVersionedTextDocumentIdentifier` rejected by a client for closed feature files | Covered by test condition 5; fallback to `Changes` if a client proves strict. |
| RA-4 | Cross-project "confirm" toggle scope | Deferred — ship with `NeedsConfirmation=false`; the builder already exposes the hook. |

---

## 10. Worked example — `WorkspaceEdit` payload (before / after)

**Today (`Changes` shape):**

```jsonc
{
  "changes": {
    "file:///c:/proj/Features/Calc.feature": [
      { "range": { "start": {"line":7,"character":4}, "end": {"line":7,"character":22} },
        "newText": "I sum two numbers" }
    ],
    "file:///c:/proj/Steps/CalcSteps.cs": [
      { "range": { "start": {"line":18,"character":16}, "end": {"line":18,"character":36} },
        "newText": "\"I sum two numbers\"" }
    ]
  }
}
```

**After (annotated `DocumentChanges` shape):**

```jsonc
{
  "documentChanges": [
    { "textDocument": { "uri": "file:///c:/proj/Features/Calc.feature", "version": null },
      "edits": [
        { "range": { "start": {"line":7,"character":4}, "end": {"line":7,"character":22} },
          "newText": "I sum two numbers",
          "annotationId": "reqnroll.rename.feature" }
      ] },
    { "textDocument": { "uri": "file:///c:/proj/Steps/CalcSteps.cs", "version": null },
      "edits": [
        { "range": { "start": {"line":18,"character":16}, "end": {"line":18,"character":36} },
          "newText": "\"I sum two numbers\"",
          "annotationId": "reqnroll.rename.binding" }
      ] }
  ],
  "changeAnnotations": {
    "reqnroll.rename.feature": { "label": "Rename step usages → \"I sum two numbers\"", "needsConfirmation": false },
    "reqnroll.rename.binding": { "label": "Update step-definition attribute", "needsConfirmation": false }
  }
}
```

The reshape is purely structural: identical ranges and `newText`, now grouped under document edits and tagged with annotation ids that key into the `changeAnnotations` catalogue.
