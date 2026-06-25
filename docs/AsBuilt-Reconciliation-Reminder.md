# ⚠️ As-Built Reconciliation — Fold Back Into the Design Docs

> **Status:** Open action — do **not** delete until every box below is checked.
> **Why this exists:** The three protocol-upgrade plans below are standalone *implementation* plans. The canonical design lives in the four `docs/LSP-IDE-Support-*` documents. When the work lands, the as-built reality must be folded back into the canonical set so the design docs do not rot — and so the next contributor (or the next AI session) reads truth, not intent.
>
> **Trigger:** Open this file when all three features have shipped (or any one ships independently). This is a post-implementation chore, not a pre-implementation one.

---

## The three plans this covers

| Plan | Protocol origin |
|---|---|
| [Rename-ChangeAnnotations-Implementation-Plan.md](Rename-ChangeAnnotations-Implementation-Plan.md) | LSP 3.16 `ChangeAnnotation` / `AnnotatedTextEdit` |
| [InlayHints-Implementation-Plan.md](InlayHints-Implementation-Plan.md) | LSP 3.17 `textDocument/inlayHint` |
| [PullDiagnostics-Implementation-Plan.md](PullDiagnostics-Implementation-Plan.md) | LSP 3.17 `textDocument/diagnostic` |

Supporting analysis: the OmniSharp library tops out at **LSP 3.17** (`OmniSharp.Extensions.LanguageServer 0.19.9`), so these three are "free" (modelled in the library); anything 3.18+ would be hand-rolled. Capture that fact in the architecture doc too (see below).

---

## Reconciliation checklist

### 1. `LSP-IDE-Support-Feature-Designs.md` (per-feature canonical design)
- [ ] Add an as-built §entry for **Rename change annotations** (extends the existing F16 §): final `WorkspaceEdit` shape, negotiation fallback, undo-unit behaviour as actually observed in VS.
- [ ] Add a new feature § for **Inlay hints**: final label format (lift the §11 visual spec + the shipped mockup image), settings defaults, resolve-vs-eager split as built.
- [ ] Add a new feature § for **Pull diagnostics**: the push⊕pull state machine as shipped, the `auto` default decision, and the workspace-report scope.
- [ ] Move each plan's "Risks & open questions" into the design doc's deferred/known-limitations sections, resolved or carried forward.

### 2. `LSP-IDE-Support-Architecture.md` (module/component inventory)
- [ ] Register new components: `WorkspaceEditBuilder`, `GherkinInlayHintService` + `FeatureInlayHintHandler`, `FeatureDiagnosticsComputer` + the two diagnostic handlers + `DiagnosticsRefreshHandler`.
- [ ] Note the diagnostics transport change (push → push/pull negotiated) in the cross-cutting "diagnostics" description.
- [ ] Record the **library ceiling = LSP 3.17** fact and the implication that 3.18+ features require custom DTOs.

### 3. `LSP-IDE-Support-Overview.md` (scope / roadmap)
- [ ] Add the three features to the feature index / phase roadmap (assign F-numbers if the F-series convention continues).
- [ ] Update the cross-client capability story (the §7 matrices) in the release-strategy section.

### 4. `LSP-IDE-Support-Open-Questions.md` (Q-register / risks)
- [ ] Promote the per-plan risk ids (`RA-*`, `IH-*`, `PD-*`) into the Q-register, or close them with the as-built answer.
- [ ] Explicitly resolve or carry **PD-4** (pull does not fix the `(uri,project)` / Q22 ambiguity; workspace per-`(uri,project)` reporting is the registered follow-on).

### 5. Memory (`~/.claude/.../memory/`)
- [ ] Add/refresh memory entries for each shipped feature (mirroring existing `project-f*` notes), including the VS capability-verification outcome from each plan's Phase 0.
- [ ] Update [[lsp-handler-dynamic-registration]] if any of these handlers established a new registration pattern.
- [ ] Record the **OmniSharp = LSP 3.17 ceiling** as a reference memory so it isn't re-derived next session.

### 6. Housekeeping
- [ ] Once §1–§5 are done, delete (or mark **Closed**) this reminder and the three standalone plan docs, or relocate them to `docs/Archive/` per the project's superseded-doc convention.

---

_If you are an AI session reading this: the canonical design docs are the source of truth. If they disagree with these plan docs, the plan docs are stale — reconcile, don't trust them blindly._
