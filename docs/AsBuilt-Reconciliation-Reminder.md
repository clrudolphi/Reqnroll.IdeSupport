# ⚠️ As-Built Reconciliation — Fold Back Into the Design Docs

> **Status:** Open action — do **not** delete until every box below is checked.
> **Why this exists:** The three protocol-upgrade plans below are standalone *implementation* plans. The canonical design lives in the four `docs/LSP-IDE-Support-*` documents. When the work lands, the as-built reality must be folded back into the canonical set so the design docs do not rot — and so the next contributor (or the next AI session) reads truth, not intent.
>
> **Trigger:** Open this file when all three features have shipped (or any one ships independently). This is a post-implementation chore, not a pre-implementation one.
>
> **2026-07-12 review:** both remaining plans have now shipped in code, but neither has been reconciled into the canonical docs — this file's checklist below was still 100% unchecked for both. Filed [#134](https://github.com/reqnroll/Reqnroll.IdeSupport/issues/134)–[#137](https://github.com/reqnroll/Reqnroll.IdeSupport/issues/137) to track the remaining doc-writing work; checklist items below now link to their tracking issue. Two memory-only items (§5) were completed directly during this review, since they don't require a repo PR.

---

## The plans this covers

| Plan | Protocol origin | Status |
|---|---|---|
| [Rename-ChangeAnnotations-Implementation-Plan.md](Rename-ChangeAnnotations-Implementation-Plan.md) | LSP 3.16 `ChangeAnnotation` / `AnnotatedTextEdit` | **Implemented (2026-07-11, #70/#133)** — Phases A–C done; VS manual undo-granularity verification (RA-1) still open. Canonical-doc reconciliation tracked in [#134](https://github.com/reqnroll/Reqnroll.IdeSupport/issues/134). |
| [InlayHints-Implementation-Plan.md](InlayHints-Implementation-Plan.md) | LSP 3.17 `textDocument/inlayHint` | **Implemented (shipped: #43 "F23", follow-ups #57, #77)** — the plan doc's own status header is stale ("Draft for review"); the feature is fully wired up (`GherkinInlayHintService`, `FeatureInlayHintHandler`, `InlayHintRefreshHandler`). Canonical-doc reconciliation tracked in [#135](https://github.com/reqnroll/Reqnroll.IdeSupport/issues/135). |
| ~~PullDiagnostics-Implementation-Plan.md~~ | LSP 3.17 `textDocument/diagnostic` | **Abandoned (2026-06-25), moved to `docs/Archive/`** — OmniSharp 0.19.9 can't serve it (write-side JSON converters are unimplemented stubs). No as-built reconciliation applies; the abandonment decision itself is recorded in `docs/LSP-IDE-Support-Open-Questions.md` Q19. |

Supporting analysis: the OmniSharp library tops out at **LSP 3.17** (`OmniSharp.Extensions.LanguageServer 0.19.9`), so the two still-viable plans are "free" (modelled in the library); anything 3.18+ would be hand-rolled. This fact is already recorded as a Claude memory ([[omnisharp-lsp-version-ceiling]]) but still needs writing into the architecture doc itself — tracked in [#136](https://github.com/reqnroll/Reqnroll.IdeSupport/issues/136).

---

## Reconciliation checklist

### 1. `LSP-IDE-Support-Feature-Designs.md` (per-feature canonical design)
- [ ] Add an as-built §entry for **Rename change annotations** (extends the existing F16 §): final `WorkspaceEdit` shape, negotiation fallback, undo-unit behaviour as actually observed in VS. — [#134](https://github.com/reqnroll/Reqnroll.IdeSupport/issues/134)
- [ ] Add a new feature § for **Inlay hints**: final label format (lift the §11 visual spec + the shipped mockup image), settings defaults, resolve-vs-eager split as built. — [#135](https://github.com/reqnroll/Reqnroll.IdeSupport/issues/135)
- [x] ~~Add a new feature § for Pull diagnostics~~ — N/A, abandoned before implementation; nothing shipped to reconcile.
- [ ] Move each remaining plan's "Risks & open questions" into the design doc's deferred/known-limitations sections, resolved or carried forward. — folded into [#134](https://github.com/reqnroll/Reqnroll.IdeSupport/issues/134) / [#135](https://github.com/reqnroll/Reqnroll.IdeSupport/issues/135)

### 2. `LSP-IDE-Support-Architecture.md` (module/component inventory)
- [ ] Register new components: `WorkspaceEditBuilder`, `GherkinInlayHintService` + `FeatureInlayHintHandler`. The rename pipeline was further decomposed in #139/#140 — `StepRenameHandler` now delegates to `CSharpAttributeLiteralResolver`, `RenameBindingResolver`, `NewNameReconciler`, `RenamePostApplyCoordinator`, and `RenameTargetsHandler`; the component inventory should reflect this split, not just the single handler. — [#134](https://github.com/reqnroll/Reqnroll.IdeSupport/issues/134) / [#135](https://github.com/reqnroll/Reqnroll.IdeSupport/issues/135)
- [x] ~~Note the diagnostics transport change (push → push/pull negotiated)~~ — N/A, diagnostics stay push-only; abandonment is already noted in Open-Questions Q19.
- [ ] Record the **library ceiling = LSP 3.17** fact and the implication that 3.18+ features require custom DTOs. — [#136](https://github.com/reqnroll/Reqnroll.IdeSupport/issues/136)

### 3. `LSP-IDE-Support-Overview.md` (scope / roadmap)
- [ ] Add the two still-viable features to the feature index / phase roadmap (assign F-numbers if the F-series convention continues). — [#137](https://github.com/reqnroll/Reqnroll.IdeSupport/issues/137)
- [ ] Update the cross-client capability story (the §7 matrices) in the release-strategy section. — [#137](https://github.com/reqnroll/Reqnroll.IdeSupport/issues/137)

### 4. `LSP-IDE-Support-Open-Questions.md` (Q-register / risks)
- [ ] Promote the per-plan risk ids (`RA-*`, `IH-*`) into the Q-register, or close them with the as-built answer, once Rename annotations / Inlay hints ship. — both have shipped; tracked in [#134](https://github.com/reqnroll/Reqnroll.IdeSupport/issues/134) / [#135](https://github.com/reqnroll/Reqnroll.IdeSupport/issues/135)
- [x] Q19 (pull diagnostics) already resolved-as-abandoned in the Q-register (2026-07-02).

### 5. Memory (`~/.claude/.../memory/`)
- [x] Add/refresh memory entries for each shipped feature — `rename-change-annotations-phase0.md` updated to reflect the completed Phases A–C; added a new `inlay-hints-shipped.md` project memory noting F23 shipped but reconciliation is still pending (2026-07-12).
- [x] ~~Update [[lsp-handler-dynamic-registration]]~~ — N/A, neither feature established a new registration pattern (`WorkspaceEditBuilder` isn't a handler; Inlay Hints used the existing base-interface + `AddHandler<T>()` path the memory already documents).
- [x] ~~Record the OmniSharp = LSP 3.17 ceiling as a reference memory~~ — already done: [[omnisharp-lsp-version-ceiling]]. Still needs writing into the repo doc itself, tracked in [#136](https://github.com/reqnroll/Reqnroll.IdeSupport/issues/136).

### 6. Housekeeping
- [ ] Once §1–§5 are done, delete (or mark **Closed**) this reminder and the three standalone plan docs, or relocate them to `docs/Archive/` per the project's superseded-doc convention. Blocked on [#134](https://github.com/reqnroll/Reqnroll.IdeSupport/issues/134), [#135](https://github.com/reqnroll/Reqnroll.IdeSupport/issues/135), [#136](https://github.com/reqnroll/Reqnroll.IdeSupport/issues/136), [#137](https://github.com/reqnroll/Reqnroll.IdeSupport/issues/137).

---

_If you are an AI session reading this: the canonical design docs are the source of truth. If they disagree with these plan docs, the plan docs are stale — reconcile, don't trust them blindly._
