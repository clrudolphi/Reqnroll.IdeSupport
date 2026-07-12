# Inlay Hints for Feature Files — Implementation Plan

> **Status:** Implemented (shipped: #43 "F23", follow-up fixes #57, #77). Canonical-doc
> reconciliation (Feature-Designs/Architecture/Open-Questions) not yet done — tracked in
> [#135](https://github.com/reqnroll/Reqnroll.IdeSupport/issues/135).
> **Audience:** Core team contributors
> **Based on:** LSP 3.17 `textDocument/inlayHint`; reuses the binding-match cache ([[binding-match-service-plan]])
> **Library support:** Already modelled in OmniSharp.Extensions.LanguageServer 0.19.9
> (`IInlayHintsHandler`, `InlayHint`, `InlayHintKind`, `InlayHintLabelPart`, `IInlayHintResolveHandler`, workspace inlay-hint refresh) — no custom DTO plumbing.

---

## 1. Nature of the changes

A new, read-only feature: render **inline annotations on `.feature` steps** showing what each step is bound to, without leaving the feature file. Two hint kinds, both driven by data already in the match cache:

- **Binding hint** (default) — at the end of a *defined* step line, show the target method, e.g. `→ CalculatorSteps.AddNumbers`. Undefined steps show nothing (the diagnostic already covers them); ambiguous steps show `→ 2 matches`.
- **Parameter-type hint** (optional, behind a setting) — for steps with captured arguments, annotate captured spans with the bound parameter type, e.g. `50` rendered with a trailing `:int`.

This is purely additive. It introduces a new handler, a new core service that projects the existing `FeatureBindingMatchSet` into hint models, and a refresh trigger so hints update when bindings change. It mirrors the structure of the F9 document-outline feature ([`FeatureDocumentSymbolHandler`](../src/LSP/Reqnroll.IdeSupport.LSP.Server/Features/DocumentOutline/FeatureDocumentSymbolHandler.cs) + `IGherkinDocumentSymbolService`).

Because inlay hints apply only to `.feature` files, there is **no `.cs` routing conflict**, so this uses the OmniSharp **base-interface + `AddHandler<T>()`** registration path rather than manual `OnRequest` plumbing, per [[lsp-handler-dynamic-registration]].

---

## 2. Structural changes to DTOs

No new wire DTOs — the protocol types ship with the library. The response is `Container<InlayHint>?`:

```
InlayHint {
    Position    : Position             // where the hint paints
    Label       : StringOrInlayHintLabelParts
    Kind        : InlayHintKind?        // Type (param hints) or Parameter; binding hint uses Type
    Tooltip     : StringOrMarkupContent? // full signature / expression, resolved lazily
    PaddingLeft : bool
    PaddingRight: bool
    Data        : object?               // correlation token for inlayHint/resolve
}
```

**New internal model** in `LSP.Core` (so the projection is unit-testable without protocol types), converted to `InlayHint` in the handler — exactly the `GherkinDocumentSymbol → DocumentSymbol` split already used by F9:

```csharp
public sealed record GherkinInlayHint(
    GherkinRange     AnchorRange,   // step text span the hint attaches to
    InlayHintAnchor  Anchor,        // EndOfLine | AtOffset
    string           Label,
    GherkinInlayHintKind Kind,      // Binding | ParameterType | Ambiguous
    string?          Tooltip);      // method signature / expression, may be deferred
```

**Registration options** advertise resolve support:

```csharp
new InlayHintRegistrationOptions {
    DocumentSelector = FeatureSelector,        // "**/*.feature", same constant style as F9
    ResolveProvider  = true                    // tooltip/command filled on inlayHint/resolve
}
```

---

## 3. New classes and methods

| Component | Project / file | Type | Responsibility |
|---|---|---|---|
| `FeatureInlayHintHandler` | `LSP.Server/Features/InlayHints/` | `IInlayHintsHandler` (+ `IInlayHintResolveHandler`) | Handles `textDocument/inlayHint` and `inlayHint/resolve`. Pulls the buffer + match set, calls the core service, converts `GherkinInlayHint → InlayHint`. |
| `IGherkinInlayHintService` / `GherkinInlayHintService` | `LSP.Core/InlayHints/` | shared service | Projects `(DeveroomTag[] tags, FeatureBindingMatchSet matchSet, InlayHintOptions opts)` into `IReadOnlyList<GherkinInlayHint>`. No protocol or IDE dependencies — directly testable. |
| `InlayHintOptions` | `LSP.Core/InlayHints/` | record | Feature toggles: `ShowBindingTarget`, `ShowParameterTypes`. Sourced from existing config plumbing. |
| `InlayHintRefreshHandler` | `LSP.Server/Pipeline/` | `INotificationHandler<MatchCacheChangedNotification>` | On match-cache change, request `workspace/inlayHint/refresh` so the client re-pulls. Mirrors [`SemanticTokensRefreshHandler`](../src/LSP/Reqnroll.IdeSupport.LSP.Server/Pipeline/SemanticTokensRefreshHandler.cs). |

**Data source — already present, no model change.** For each `StepBindingMatch` in the requested range:

- `IsDefined` + single item → `MatchResultItem.MatchedStepDefinition` gives `StepDefinitionType`, `Expression`, and `Implementation.SourceLocation`; the method display name comes from the implementation (method/declaring-type already carried for Go-to-Definition).
- `IsAmbiguous` → label `→ {n} matches`, tooltip lists them.
- `IsUndefined` → no hint.
- Parameter-type hints use the matched binding's `Regex` capture groups against the step text span (`StepBindingMatch.Range`) to locate argument offsets.

**Handler skeleton** (mirrors F9):

```csharp
public Task<InlayHintContainer?> Handle(InlayHintParams request, CancellationToken ct)
{
    if (!_documentBufferService.TryGet(request.TextDocument.Uri, out var buffer) || buffer?.Tags is null)
        return Task.FromResult<InlayHintContainer?>(null);

    var owner    = _scopeManager.ResolvePrimaryOwner(request.TextDocument.Uri);   // Q22 primary-owner rule
    var matchSet = ResolveMatchSet(request.TextDocument.Uri, owner);
    var hints    = _service.Build(buffer.Tags, matchSet, _options)
                           .Where(h => Intersects(h.AnchorRange, request.Range)); // honour the requested window
    return Task.FromResult<InlayHintContainer?>(new InlayHintContainer(hints.Select(ToInlayHint)));
}
```

`inlayHint/resolve` defers the `Tooltip` (full signature / source path) so the initial response stays cheap on large feature files.

---

## 4. Surfacing in the Visual Studio client

**Inlay hints are rendered entirely client-side; an interceptor cannot inject rendering.** Unlike custom classifications (which we patch via `SemanticTokensClassificationInterceptor`), there is no server-side workaround if the VS LSP client does not consume `textDocument/inlayHint`. So the VS story is **capability verification first**, code second.

1. **Verify VS advertises `textDocument.inlayHint`** in its `initialize` capabilities — read the `LspInspectorLogger` session log. If absent, the server simply receives no `inlayHint` requests and the feature is dormant in VS while still working in VS Code / Rider. No harm, no gating needed for correctness.
2. **Editor setting dependency.** Even when supported, VS only paints hints when the user has inline hints enabled (Tools → Options → Text Editor → display inline hints, and/or the Alt+F1 hold-to-show gesture). Document this; it is not something the extension forces.
3. **Refresh path.** `InlayHintRefreshHandler` sends `workspace/inlayHint/refresh` (server→client) via the library's workspace facade — the same outbound-request mechanism `SemanticTokensRefreshHandler` already uses successfully through the intercepting pipe. No new pipe wiring.
4. **No new VSSDK command, no `ReqnrollLanguageClient` change.** This feature adds nothing to [`ReqnrollLanguageClient`](../src/VisualStudio/Reqnroll.IdeSupport.VisualStudio.Extension/ReqnrollLanguageClient.cs) or the interceptor array — it is a standard pull feature.
5. **If VS support proves missing or buggy**, keep the server feature enabled unconditionally (it is spec-standard); do **not** add a VS-specific shim. Any future VS-only divergence would follow [[feedback-vs-specific-gating]], but the expectation is the unmodified standard feature.

---

## 5. Impact on testing

### 5.1 Approach

- **Core unit** — `GherkinInlayHintServiceTests`: the bulk of the logic. Feed hand-built `DeveroomTag[]` + `FeatureBindingMatchSet` (constructed directly, per [[core-tests-avoid-stubidescope]]) and assert the projected `GherkinInlayHint` list. No protocol, no IDE.
- **Server unit** — `FeatureInlayHintHandlerTests`: range-window filtering, primary-owner resolution, tag/match-set absence, and `GherkinInlayHint → InlayHint` conversion (positions, padding, kind). Plus `inlayHint/resolve` populates the tooltip.
- **Acceptance (specs)** — a new `.feature` spec via `LspServerHarness`: open a feature with defined/undefined/ambiguous steps and a parameterised step, request `textDocument/inlayHint`, assert the hint set.
- **Refresh** — `InlayHintRefreshHandlerTests`: a `MatchCacheChangedNotification` triggers exactly one `workspace/inlayHint/refresh`.

### 5.2 Test conditions

| # | Condition | Expected |
|---|---|---|
| 1 | Defined step, unique binding | One `Binding` hint at end-of-line; label = target method; kind `Type` |
| 2 | Undefined step | No hint |
| 3 | Ambiguous step (≥2 matches) | `→ {n} matches` hint; tooltip enumerates the candidates |
| 4 | Parameterised step, `ShowParameterTypes = true` | Type hint at each captured-argument offset; correct `:type` from the regex group |
| 5 | `ShowParameterTypes = false` | No parameter-type hints; binding hints unaffected |
| 6 | `ShowBindingTarget = false` | No binding hints |
| 7 | Request `range` covers only part of the file | Only hints whose anchor intersects `range` are returned |
| 8 | Background + Scenario Outline steps | Hints produced; outline example placeholders do not crash projection |
| 9 | Registry `Invalid` (Connector not ready, Q24) | Empty match set → no hints (no spurious output) |
| 10 | `inlayHint/resolve` on a hint with deferred tooltip | Tooltip populated; `Data` round-trips |
| 11 | Shared/linked feature, multiple owners | Primary owner's match set used (Q22), single coherent hint set |
| 12 | `MatchCacheChangedNotification` | Exactly one `workspace/inlayHint/refresh` emitted |

**Performance note for tests:** assert the initial `inlayHint` response does **not** compute tooltips (they belong to resolve) so large-file latency stays bounded.

---

## 6. Phased build plan & effort

### Phase 0 — Capability verification (go/no-go) · ~0.5 day

- In the experimental VS instance, open a `.feature` file with defined steps and confirm whether VS sends `textDocument/inlayHint` at all (inspector log). Check `capabilities.textDocument.inlayHint != null`.
- **Decision rule:**
  - Advertised → full scope; hints render in VS.
  - Not advertised → still ship (VS Code/Rider benefit); VS stays dormant with zero harm. Record in §7. Do **not** build a VS shim — rendering is client-side and cannot be intercepted.

### Phase A — `GherkinInlayHintService` (Core) (~1.5 days)
The bulk of the logic and tests, with no protocol/IDE dependency. Binding-target hints first; parameter-type hints behind the toggle.

### Phase B — `FeatureInlayHintHandler` + resolve + registration (~1 day)
Base-interface handler, `AddHandler<T>()` wiring, range-window filtering, `inlayHint/resolve` tooltip deferral.

### Phase C — `InlayHintRefreshHandler` + settings + tests (~1 day)
Refresh on `MatchCacheChangedNotification`; config toggles; spec + unit conditions (§5); VS verification.

**Total: ~4 days.**

---

## 7. Cross-client support matrix

| Client | Behaviour | Notes |
|---|---|---|
| VS Code | Full | Honours `inlayHint/resolve`; user toggles via editor inlay-hint settings |
| Rider | Full | JetBrains LSP inlay-hint support |
| Visual Studio | TBD by Phase 0 | If supported, requires the editor's inline-hints display gesture/setting enabled; otherwise dormant |

---

## 8. Telemetry

| Event / property | Type | Purpose |
|---|---|---|
| `InlayHintRequested` · `HintCount` | counter | Field signal that VS actually pulls hints (answers the Phase-0 question continuously). |
| `InlayHintRequested` · `ElapsedMs` | duration | Watch projection cost on large files; informs the resolve-vs-eager split. |
| `InlayHintResolved` | counter | How often tooltips are actually requested — justifies the deferral. |

Emit via `ILspTelemetryService`, sampled (hints fire on every scroll — do **not** emit per request unbatched).

---

## 9. Settings surface

Two toggles, sourced from the existing config plumbing (`InlayHintOptions`):

| Setting | Default | Rationale |
|---|---|---|
| `reqnroll.inlayHints.showBindingTarget` | **on** | The primary value — "what is this step bound to" |
| `reqnroll.inlayHints.showParameterTypes` | **off** | Denser; opt-in to avoid clutter on parameter-heavy suites |

Document that VS additionally requires its own editor-level inline-hints display setting; our toggles only control what the *server* offers.

---

## 10. Performance & scale

- **Honour `request.Range`** — only project hints whose anchor intersects the requested window; never build the whole file when the client asks for a viewport.
- **Honour the cancellation token** in both projection and resolve — inlay requests are superseded rapidly during scrolling.
- **Debounce refresh** — coalesce bursts of `MatchCacheChangedNotification` into a single `workspace/inlayHint/refresh` (the match cache can churn during discovery). Reuse the debounce approach already used for semantic-token refresh if present.
- **Defer tooltips** to `inlayHint/resolve` (already in design) so the hot path stays span-math only.
- Establish a soft budget (e.g. hint projection for a viewport ≤ a few ms) and assert it loosely in tests via the telemetry duration.

---

## 11. Visual specification

Target rendering (binding-target hints on; parameter-type hints on for illustration). Hints are dimmed, non-editable, painted after the step text:

```text
  Scenario: Add two numbers
    Given I have entered 50 into the calculator        → CalculatorSteps.EnterNumber   50ːint
    And I have entered 70 into the calculator          → CalculatorSteps.EnterNumber   70ːint
    When I press add                                   → CalculatorSteps.PressAdd
    Then the result should be 120 on the screen        → 2 matches                     120ːint
    And an undefined step with no binding
```

Rules:
- **Binding hint** anchors at end-of-line: `→ {DeclaringType}.{Method}`; ambiguous → `→ {n} matches`; undefined → nothing.
- **Parameter-type hint** anchors immediately after each captured argument span: `{arg}ː{type}` (`PaddingLeft = false`, no space before the separator).
- Truncate long type names / method paths to a max width with an ellipsis; full text lives in the resolved tooltip.
- Labels are not localized (type/identifier names are language-neutral); the `→` / `matches` connective text is the only localizable surface.

A polished mockup of this rendering accompanies the review; reproduce it here as an image when the doc is finalized.

---

## 12. Risks & open questions

| # | Item | Disposition |
|---|---|---|
| IH-1 | VS does not consume `textDocument/inlayHint` | Resolved by Phase 0; feature dormant in VS, no shim. |
| IH-2 | Conflict with a VS built-in inline-hint provider over `.feature` | Unlikely (no built-in Gherkin hint provider); confirm during Phase C. |
| IH-3 | Parameter offset mapping when the regex capture spans differ from the visible step text (placeholders in Scenario Outline) | Covered by test condition 8; fall back to no param hint rather than a wrong span. |
| IH-4 | Refresh storms during discovery | Mitigated by debounce (§10); verify a single refresh per settled cache. |
