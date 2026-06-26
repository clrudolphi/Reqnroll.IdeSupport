# F7 (Keyword Completion) & F8 (Step Completion) — Design & Implementation Plan

## Context

The new LSP server (`Reqnroll.IdeSupport.LSP.Server`) re-implements the IDE features that the
legacy Visual Studio extension (`Reqnroll.VisualStudio`) provided via MEF, but now over the LSP
wire so VS Code, Visual Studio and Rider share one engine. Features **F7 (keyword completion)**
and **F8 (step completion)** are still unimplemented — there is *no* production completion code in
the repo (only a leftover `StepDefinitionSamplerTests.cs` under `tests/VisualStudio/...` that
references the legacy namespace). The design docs specify both features
(`docs/LSP-IDE-Support-Feature-Designs.md` §F7/§F8) and leave the matching strategy open as **Q14**
(`docs/LSP-IDE-Support-Open-Questions.md`).

This plan delivers both features as **two increments** (F7 first = doc Phase 3, F8 second = Phase 4),
reusing the proven keyword/sampler logic from the legacy extension and resolving Q14 with a
**pluggable matcher** whose default uses **FuzzySharp** for server-side ranking.

### Decisions taken (confirmed with user)
- **Matching (Q14):** Ship **client-side (built-in) matching first** — the server returns all valid
  same-type step samples and the LSP client does the filtering/sorting. This is validated by **manual
  testing**. Only **if that proves unacceptable** do we adopt **FuzzySharp** server-side ranking. The
  `ICompletionMatcher` abstraction is introduced now so the swap is a one-line DI change, but the
  default registration is the trivial return-all matcher.
  - **FuzzySharp ranking spec (contingency):** sort candidates in two tiers —
    **(1)** samples whose text *starts-with* the typed text (case-insensitive), sorted by
    **frequency of use** (descending); **(2)** the remaining fuzzy matches, sorted by a weighted score
    of **60 % fuzzy-match score + 40 % frequency of use**. "Frequency of use" = the number of feature
    steps currently bound to that step definition (the same count F18 codeLens shows, via
    `IBindingMatchService.FindUsages`).
- **Insert format:** Literal sample text (e.g. `I have entered [int] into the calculator`) — full
  parity with the legacy `StepDefinitionSampler`. No snippet placeholders.
- **Scope/phasing:** F7 delivered first, F8 (incl. sampler port + FuzzySharp) as a separate second
  increment.

---

## Architecture & key existing components to reuse

| Concern | Existing component | Path |
|---|---|---|
| LSP request wiring | **Preferred:** OmniSharp base interface + `options.AddHandler<T>()` with a `**/*.feature` document selector (dynamic registration) — as `FeatureDefinitionHandler` (`IDefinitionHandler`) does. Manual `options.OnRequest` + static capability is used *only* for handlers that also touch `.cs` (semantic tokens, references, F18 codeLens) to avoid C#-server ambiguity. | `src/LSP/Reqnroll.IdeSupport.LSP.Server/Program.cs` (`AddHandler<FeatureDefinitionHandler>()`) |
| Document text + parsed tags | `IDocumentBufferService` → `DocumentBuffer` (`Text`, `Version`, `Tags`) | `.../LSP.Server/Services/DocumentBufferService.cs` |
| Snapshot + offset/line | `DocumentBuffer.ToGherkinTextSnapshot()`, `IGherkinTextSnapshot.ToOffset(line,char)`, `GetLineFromLineNumber` | `.../Services/DocumentBufferExtensions.cs`, `.../Document/GherkinRangeExtensions.cs`, `.../Document/LspTextSnapshot.cs` |
| Gherkin parse (on demand) | `IDeveroomTagParser.Parse(snapshot, registry)` — synchronous; emits `Document` tag (carries `DeveroomGherkinDocument`), `StepBlock` tags (carry `DeveroomGherkinStep`) | `.../LSP.Core/Editor/Services/Parsing/GherkinDocuments/DeveroomTagParser.cs` |
| Expected keyword tokens | `DeveroomGherkinDocument.GetExpectedTokens(line, monitoringService)` + `GherkinDialect` | `.../LSP.Core/.../GherkinDocuments/DeveroomGherkinDocument.cs` |
| Dialect fallback by config | `ReqnrollGherkinDialectProvider.GetDialect(lang)`; config via `ILspWorkspaceScopeManager.GetConfigurationProviderForUri(uri)` | `.../LSP.Core/.../GherkinDocuments/ReqnrollGherkinDialectProvider.cs` |
| Step definitions for a feature URI | `IProjectBindingRegistryLookup.GetRegistryForUri(uri)` → `ProjectBindingRegistry.StepDefinitions` (each `ProjectStepDefinitionBinding`: `StepDefinitionType`, `Expression`, `IsValid`, `Implementation.ParameterTypes`) | `.../LSP.Server/Discovery/IProjectBindingRegistryLookup.cs`, `.../LSP.Core/Discovery/ProjectStepDefinitionBinding.cs` |
| Step sample generation (PORT) | `StepDefinitionSampler` + `RegexStepDefinitionExpressionAnalyzer` + `AnalyzedStepDefinitionExpression*` | legacy `Reqnroll.VisualStudio/Editor/Completions/` & `.../Editor/Services/StepDefinitions/` |

**Reference handler to mirror:** `FeatureDefinitionHandler` (F5, `IDefinitionHandler` registered via
`AddHandler` with a feature-file selector) for the registration shape; `GoToStepDefinitionsHandler` /
`StepCodeLensHandler` for the constructor-injection + URI-guard body shape.

---

## Increment 1 — F7 Keyword Completion

### New files
- `src/LSP/Reqnroll.IdeSupport.LSP.Core/Editor/Completions/ICompletionService.cs`
- `src/LSP/Reqnroll.IdeSupport.LSP.Core/Editor/Completions/CompletionService.cs` — VS-free, netstandard2.0, testable.
- `src/LSP/Reqnroll.IdeSupport.LSP.Server/Handlers/ProtocolHandlers/GherkinCompletionHandler.cs` — thin LSP adapter.

### CompletionService (keyword branch)
Port the keyword logic from legacy `DeveroomCompletionSource`:
- `AddCompletionsFromExpectedTokens(TokenType[], dialect)` — maps `TokenType.FeatureLine/RuleLine/
  BackgroundLine/ScenarioLine/ExamplesLine/StepLine/DocStringSeparator/TableRow/Language/TagLine` to the
  dialect's keyword arrays (`FeatureKeywords`, `ScenarioKeywords`+`ScenarioOutlineKeywords`,
  `GivenStepKeywords`/`When`/`Then` with `RemoveBulletKeyword`, `And`/`But`, etc.), each with a human
  description and postfix (`": "` for block keywords).
- `GetDefaultKeywordCompletions(dialect)` — fallback when no parsed document / no expected tokens
  (all `StepKeywords` + `GetBlockKeywords()`), matching legacy `AddDefaultKeywordCompletions`.
- Returns a framework-neutral `CompletionResult` (list of `CompletionEntry { Label, Detail, Kind,
  SortText?, FilterText? }` + an optional replacement `range`), so Core stays free of OmniSharp types.
  Keyword `Kind = Keyword`.

### GherkinCompletionHandler (F7 path)
Implements OmniSharp `ICompletionHandler` (mirrors `FeatureDefinitionHandler`'s `IDefinitionHandler`
shape). `GetRegistrationOptions(CompletionCapability, ClientCapabilities)` returns
`CompletionRegistrationOptions` with `DocumentSelector = new TextDocumentSelector(new TextDocumentFilter
{ Pattern = "**/*.feature" })`, `ResolveProvider = false`, and `TriggerCharacters` empty (rely on
client auto-trigger + explicit invoke). `Handle(CompletionParams, CancellationToken)`:
1. Guard `IsFeatureFile(uri)`; else return empty `CompletionList`.
2. `TryGet` buffer; build snapshot; compute `offset` and the cursor line via `ToOffset` /
   `GetLineFromLineNumber`.
3. Parse fresh tags on demand: `registry = _registryLookup.GetRegistryForUri(uri);
   tags = _tagParser.Parse(snapshot, registry)` — avoids the stale-`buffer.Tags` race during fast
   typing (the buffer's tags can lag the latest version). Extract the `Document` tag's
   `DeveroomGherkinDocument`.
4. Dialect: `doc?.GherkinDialect` ?? `ReqnrollGherkinDialectProvider.GetDialect(config.DefaultFeatureLanguage)`
   using `GetConfigurationProviderForUri(uri)`.
5. `tokens = doc.GetExpectedTokens(line, NullMonitoringService.Instance)`; call
   `CompletionService.GetKeywordCompletions(tokens, dialect)`.
6. Compute the replacement range (port `GetKeywordCompletionSpan`: from first non-whitespace on the
   line, over the current word + trailing whitespace) and map each `CompletionEntry` →
   OmniSharp `CompletionItem`. Return `new CompletionList(items)`.

### Registration (Program.cs)
- **Preferred — dynamic registration via OmniSharp base class**, matching `FeatureDefinitionHandler`:
  ```csharp
  options.Services.AddSingleton<GherkinCompletionHandler>();   // DI (handler holds no ILanguageServer ref)
  options.AddHandler<GherkinCompletionHandler>();              // alongside AddHandler<FeatureDefinitionHandler>()
  ```
  The `**/*.feature` document selector keeps completion scoped to feature files, so there is no
  dynamic-registration conflict with the C# language server (unlike semantic tokens / references /
  codeLens, which is why *those* use manual `OnRequest`).
- Register `ICompletionService → CompletionService`, `ICompletionMatcher → ReturnAllCompletionMatcher` in DI.
- **Fallback (only if proven necessary):** should the VS LSP client reject dynamic completion
  registration the way it does semantic tokens, fall back to static capability
  (`response.Capabilities.CompletionProvider = new CompletionRegistrationOptions.StaticOptions { … }`)
  in `OnInitialized` + a manual `options.OnRequest<CompletionParams, CompletionList?>("textDocument/
  completion", …)`. Do not adopt this pre-emptively — verify the dynamic path against VS first.

---

## Increment 2 — F8 Step Completion

### Port the sampler (no rewrite)
Copy these legacy classes into
`src/LSP/Reqnroll.IdeSupport.LSP.Core/Editor/Completions/` (and `.../Editor/Services/StepDefinitions/`),
adjusting namespaces to `Reqnroll.IdeSupport.LSP.Core.*`:
- `StepDefinitionSampler.cs`
- `RegexStepDefinitionExpressionAnalyzer.cs`, `IStepDefinitionExpressionAnalyzer`,
  `AnalyzedStepDefinitionExpression(+Part/SimpleTextPart/ParameterPart/WithOperatorsTextPart)`.

They are pure string/regex logic and depend only on `ProjectStepDefinitionBinding`,
`ProjectBindingImplementation`, `TypeShortcuts` — all already present in `LSP.Core/Discovery`. The
existing `tests/VisualStudio/.../StepDefinitionSamplerTests.cs` can be moved to LSP.Core.Tests and
re-pointed at the new namespace (its cases become the sampler's regression suite verbatim).

### Pluggable matcher (resolves Q14)
- `src/LSP/Reqnroll.IdeSupport.LSP.Core/Editor/Completions/Matching/ICompletionMatcher.cs`
  ```csharp
  public readonly record struct StepCandidate(string Sample, int UsageCount);  // UsageCount = frequency of use
  public readonly record struct ScoredCandidate(string Sample, double Score);

  public interface ICompletionMatcher
  {
      // Returns candidates ranked best-first. Implementations may trim.
      IReadOnlyList<ScoredCandidate> Rank(string typed, IReadOnlyList<StepCandidate> candidates);
      bool IsIncomplete { get; } // true => server asks client to re-query on each keystroke
  }
  ```
- **`ReturnAllCompletionMatcher` (DEFAULT, shipped first):** no filtering/ranking; returns candidates
  in a stable order, `IsIncomplete = false`. The client does all filtering/sorting. This is the
  build we manually test first. `AddSingleton<ICompletionMatcher, ReturnAllCompletionMatcher>()`.
- **`FuzzyCompletionMatcher` (CONTINGENCY, swap-in):** uses **FuzzySharp**; implements the two-tier
  ranking exactly:
  1. **Tier 1 — starts-with:** candidates whose `Sample` starts with `typed` (case-insensitive),
     ordered by `UsageCount` descending (ties → alphabetical). These always rank above Tier 2.
  2. **Tier 2 — fuzzy:** remaining candidates scored with `Fuzz.WeightedRatio(typed, Sample)`
     (0..100; token-aware, handles reordering + typos). Drop those below a threshold (e.g. 45,
     tunable). Final sort key = `0.6 * fuzzyScore + 0.4 * normFreq`, descending, where
     `normFreq = 100 * UsageCount / maxUsageCount` across the candidate set (0 when `maxUsageCount == 0`).
  3. Empty `typed` ⇒ skip fuzzy; return all ordered by `UsageCount` desc (Tier-1 rule with empty
     prefix). Cap the combined result at N (e.g. 50). `IsIncomplete = true`.
  - Adopt by changing the single DI line to `FuzzyCompletionMatcher` (+ the package reference below).

> **FuzzySharp notes (only added when the contingency triggers):** MIT-licensed .NET port of Python
> `fuzzywuzzy`; targets `netstandard2.0` (compatible with LSP.Core). Add
> `<PackageReference Include="FuzzySharp" Version="2.*" />` to `Reqnroll.IdeSupport.LSP.Core.csproj`,
> pinning the exact version in CI. Only `Fuzz.WeightedRatio` is used, so the dependency stays narrow
> and fully wrapped behind `ICompletionMatcher`.

### CompletionService (step branch)
- `GetStepCompletions(DeveroomGherkinStep step, string typedAfterKeyword, registry, usageCounter, matcher)`:
  1. Filter `registry.StepDefinitions.Where(sd => sd.IsValid && sd.StepDefinitionType == step.ScenarioBlock)`
     (exact legacy filter; `ScenarioBlock` resolved from the step's keyword by the parser).
  2. For each binding build a `StepCandidate(Sample, UsageCount)`:
     - `Sample = sampler.GetStepDefinitionSample(sd)` (deduplicate by Sample).
     - `UsageCount` = **frequency of use** = `usageCounter(sd)` →
       `IBindingMatchService.FindUsages(sd.Implementation.SourceLocation, projectFilter).Count`,
       i.e. the same usage count F18 codeLens computes. `projectFilter` derived from
       `ILspWorkspaceScopeManager.ResolveOwners(uri)` (Q22 per-owner scoping, identical to
       `StepCodeLensHandler`). The default `ReturnAllCompletionMatcher` ignores `UsageCount`; only
       the FuzzySharp contingency reads it — but the service always populates it so the swap needs no
       further wiring.
  3. `matcher.Rank(typedAfterKeyword, candidates)` → ordered `CompletionEntry` list with
     `Kind = CompletionItemKind.Text`, `InsertText = Sample` (literal), `FilterText = Sample`,
     `SortText = zero-padded rank` (so the server's order survives client re-sorting when ranking is on).
  4. Return `CompletionResult { IsIncomplete = matcher.IsIncomplete, Entries, ReplacementRange }`.
- The handler therefore injects `IBindingMatchService` and `ILspWorkspaceScopeManager` in addition to
  the F7 dependencies (both already singletons in `Program.cs`).

### GherkinCompletionHandler (F8 dispatch)
Extend `HandleAsync` to branch like legacy `DeveroomCompletionSource.CollectCompletions`:
- From parsed tags find the `StepBlock` tag covering the line → `DeveroomGherkinStep`.
- If a step is present **and** cursor offset ≥ step-text start (`line.Start + (step.Location.Column-1)
  + step.Keyword.Length`): take the substring from step-text-start to cursor as `typedAfterKeyword`,
  call the **step** branch, and set the replacement range to (step-text-start … line end)
  (port `GetStepCompletionSpan`).
- Otherwise fall through to the **F7 keyword** branch.
- Propagate `IsIncomplete` onto the returned `CompletionList`.

(No new LSP registration needed — F8 reuses the `textDocument/completion` route from Increment 1.)

---

## Performance, cancellation & debounce

**Do we need debounce for completion? — No artificial server-side debounce; use cancellation +
version-keyed caching instead.** Reasoning:

- **Completion is a pull request, not a push.** The LSP client (VS, VS Code) already debounces and
  throttles `textDocument/completion` and **cancels superseded requests**. The server must respond
  promptly; delaying it would only add latency. This is unlike the repo's existing debounced paths
  (`ConnectorBindingRegistryProvider`, `SemanticTokensRefreshHandler`, `LspWorkspaceScopeManager`),
  which debounce *server-initiated* recompute/push after bursts of `didChange`. Completion simply
  *reads* the state those paths maintain.
- **Honor the `CancellationToken`.** OmniSharp passes a token to `ICompletionHandler.Handle`; check it
  before/after the on-demand parse and before building items so a keystroke-superseded request bails
  early. (The `didChange` pipeline already threads the token the same way.)
- **The real cost is repeated work per keystroke, addressed by caching — especially in the FuzzySharp
  `IsIncomplete=true` path, which forces a client re-query on every keystroke:**
  - *Parse reuse:* prefer `buffer.Tags` when the buffer's captured snapshot version equals the current
    snapshot version; only parse on demand when they differ (the staleness case). Optionally memoize
    the parsed `Document` tag by `(uri, version)`.
  - *F8 candidate reuse:* memoize the `(Sample, UsageCount)` candidate list per
    `(registry.Version, owner, ScenarioBlock)` so only `ICompletionMatcher.Rank` re-runs per keystroke;
    `FindUsages` (the frequency source) is recomputed only when the registry/match cache changes —
    invalidate on the existing `BindingRegistryChangedNotification`.
- **Optional churn reduction:** in fuzzy mode, once the trimmed candidate set is small the matcher may
  return `IsIncomplete=false`, letting the client filter the remaining set without re-querying.

Net: no debounce timer is added for F7/F8; cancellation + version-keyed memoization keep the
per-keystroke path cheap. Revisit only if profiling on large feature files / large registries shows a
hot path.

---

## Deviations from the existing F7/F8 design (highlight)

1. **Handler shape & registration — aligned with design (no deviation).** Per user preference, the
   handler uses the OmniSharp `ICompletionHandler` base interface with **dynamic registration**
   (`AddHandler` + `**/*.feature` selector), exactly as the design-doc diagrams imply and as
   `FeatureDefinitionHandler` already does. Manual `OnRequest` is reserved as a fallback only if VS is
   proven to reject dynamic completion registration.
2. **Q14 — client-side matching first, FuzzySharp as a defined contingency.** The legacy VS extension
   used client-side word-contains (`WordContainsFilteredCompletionSet`). **Default here matches that
   spirit:** ship `ReturnAllCompletionMatcher` and let the LSP client filter; validate by manual test.
   The contingency `FuzzyCompletionMatcher` (server-side, `IsIncomplete=true`) is only adopted if the
   built-in matching is unacceptable, and uses the two-tier ranking (starts-with by frequency, then
   60 % fuzzy / 40 % frequency). Pluggable behind `ICompletionMatcher`, so the swap is one DI line.
3. **`IsIncomplete` re-query loop (contingency only).** When the FuzzySharp matcher is active it trims
   the list, so the server marks the `CompletionList` incomplete to force a per-keystroke re-query. The
   default return-all matcher leaves `IsIncomplete=false` (single fetch, client filters) — matching the
   MEF design.
4. **On-demand parse in the handler.** Rather than read possibly-stale `buffer.Tags`, the handler
   re-parses the current snapshot synchronously (`IDeveroomTagParser.Parse`) for race-free keyword
   context. Legacy used a live VS tag aggregator; the LSP buffer's tags are produced asynchronously.
5. **Core/Server split.** All matching/sampling/keyword logic lives in `LSP.Core` (netstandard2.0,
   VS-free, unit-testable without `StubIdeScope`); the Server handler is a thin adapter. Legacy had the
   logic inside the VS-coupled `DeveroomCompletionSource`.
6. **No snippet placeholders.** Confirmed: insert literal sample text (`[int]`) for full parity with
   `StepDefinitionSampler`, deferring snippet tab-stops as possible future work.
7. **`completionItem/resolve` deferred.** Design lists lazy resolve; we set `ResolveProvider=false`
   and put detail (step type / method) directly on items. Can be added later if items get heavy.

---

## Test inventory

### Unit tests (xUnit + AwesomeAssertions, `tests/LSP/Reqnroll.IdeSupport.LSP.Core.Tests`)

**F7 — `Editor/Completions/CompletionServiceKeywordTests.cs`**
- StepLine token ⇒ offers Given/When/Then/And/But (bullet `*` keyword removed).
- FeatureLine ⇒ `Feature: `; ScenarioLine ⇒ both Scenario and Scenario Outline; ExamplesLine,
  BackgroundLine, RuleLine, TagLine, DocStringSeparator, TableRow, Language each produce their set.
- No expected tokens ⇒ default keyword set (all step keywords + block keywords).
- **Dialect awareness:** German dialect ⇒ `Angenommen/Wenn/Dann` not `Given/When/Then`.
- Descriptions/postfixes present and correct.

**F8 — port `StepDefinitionSamplerTests.cs`** (move + re-namespace) — keep all existing cases:
simple text, `[int]`/`[string]`/custom type placeholders, `???` for missing param types, unescaping,
nested groups, fallback-to-regex on operators, choice-parameter preservation.

**F8 — `Editor/Completions/CompletionServiceStepTests.cs`**
- Filters by `ScenarioBlock` (Given step ⇒ only Given bindings); excludes `!IsValid` bindings.
- Empty typed text ⇒ all same-type samples returned.
- Returns literal sample insert text; sets `FilterText`/`SortText`.
- Deduplicates identical samples.
- **Frequency wiring:** each candidate's `UsageCount` comes from the injected usage counter
  (`IBindingMatchService.FindUsages` fake) with the resolved owner filter — assert the count is passed
  through to the matcher for the right binding.

**F8 — `Editor/Completions/Matching/ReturnAllCompletionMatcherTests.cs` (DEFAULT)**
- Returns input unfiltered, stable order preserved, `UsageCount` ignored, `IsIncomplete == false`.

**F8 — `Editor/Completions/Matching/FuzzyCompletionMatcherTests.cs` (CONTINGENCY)**
- **Tier 1 precedence:** a starts-with candidate always ranks above any non-starts-with fuzzy
  candidate, even one with a higher raw fuzzy score.
- **Tier 1 ordering:** among starts-with candidates, higher `UsageCount` ranks first (ties →
  alphabetical).
- **Tier 2 weighting:** ordering follows `0.6*fuzzyScore + 0.4*normFreq`; a frequency-heavy candidate
  can overtake a slightly-better-fuzzy-but-rare one, and vice-versa (table-driven cases pinning the
  formula). `normFreq` normalization by `maxUsageCount`; `maxUsageCount == 0` ⇒ `normFreq == 0`.
- Word-reordered query still matches (WeightedRatio); single-char typo tolerated; below-threshold
  candidates dropped; result capped at N; empty query ⇒ all retained ordered by `UsageCount` desc;
  `IsIncomplete == true`. Deterministic ordering for ties.

**Handler-level (optional, `tests/.../LSP.Server.Tests` if present, else covered by specs)**
- `GherkinCompletionHandlerTests.cs` — non-`.feature` URI ⇒ empty; missing buffer ⇒ empty; cursor on
  step text ⇒ step branch; cursor at line start ⇒ keyword branch; replacement range correctness.

### Spec / acceptance tests (Reqnroll, `tests/LSP/Reqnroll.IdeSupport.LSP.Server.Specs`)

New feature files under `Features/Editor/` (new subfolder), driven through the real in-process
server via `LspServerHarness`/`LspScenarioContext`:

**`Features/Editor/KeywordCompletion.feature` (F7)**
- Completion at start of a step line returns Given/When/Then keyword items.
- Completion on the Feature line returns `Feature:`.
- Inside a Scenario Outline, completion offers `Examples:`; `Background:` only at feature level.
- German-language feature file returns localized keywords. *(design doc F7)*

**`Features/Editor/StepCompletion.feature` (F8)**
- With a discovered Given binding `I have entered (.*) into the calculator`, completion after
  `Given ` includes `I have entered [int] into the calculator`.
- Step-type filtering: a When binding is not offered after `Given `.
- Partial text `entered` still surfaces the matching step (fuzzy/token-set), demonstrating ranking.
- No bindings discovered ⇒ empty step list (no crash), keyword fallback still works on empty lines.
  *(design doc F8 / Q14)*

**Supporting test infrastructure**
- `LspClientExtensions.RequestCompletionAsync(uri, line, char)` → wraps `textDocument/completion`
  returning `CompletionList?` (mirror existing `RequestCodeLensAsync`).
- New step bindings in `StepDefinitions/` (e.g. `CompletionSteps.cs`): "completion is requested at
  line X column Y in \"file\"", "the completion items include \"…\"", "the completion items do not
  include \"…\"". Reuse `CSharpBindingSteps`/fixture bindings to seed step definitions for F8.
- Store last `CompletionList` on `LspScenarioContext` (`LastCompletions`).

---

## Verification

1. **Build:** `dotnet build` the solution (Core targets netstandard2.0 — confirm FuzzySharp restores
   there; Server/Tests net10.0).
2. **Unit:** `dotnet test tests/LSP/Reqnroll.IdeSupport.LSP.Core.Tests` — sampler regression + service
   + matcher suites green.
3. **Specs:** `dotnet test tests/LSP/Reqnroll.IdeSupport.LSP.Server.Specs` — new
   KeywordCompletion/StepCompletion features pass end-to-end over the real LSP pipe.
4. **Manual (VS Code or VS):** open a `.feature` file in a project with discovered bindings; verify
   Ctrl+Space at line start offers keywords (and localized keywords for a non-English project), and
   typing after `Given ` offers ranked step samples that insert as literal text. Inspect the LSP trace
   to confirm `textDocument/completion` requests/responses and `isIncomplete` re-query behaviour.
5. **VS dynamic-registration check (gates the registration fallback):** confirm the dynamic
   `ICompletionHandler` registration is honoured by the Visual Studio LSP client (completion actually
   fires in a `.feature` editor). Only if it does not, switch to the static-capability +
   manual-`OnRequest` fallback noted in Registration.
6. **Matching acceptability check (gates FuzzySharp adoption):** with the default
   `ReturnAllCompletionMatcher`, manually evaluate the built-in client-side filtering/ordering in
   VS and VS Code on a project with many step definitions. **Only if unacceptable**, switch the DI
   line to `FuzzyCompletionMatcher`, add the FuzzySharp package, and re-verify the two-tier ordering
   (starts-with-by-frequency first, then 60/40 weighted) plus the `isIncomplete` re-query loop.

## Suggested commit/PR sequence
- PR 1 (F7): CompletionService keyword logic + handler + registration + F7 unit & spec tests.
- PR 2 (F8): port sampler/analyzer + `ICompletionMatcher` with `ReturnAllCompletionMatcher` default +
  step branch (incl. frequency wiring) + F8 unit & spec tests. Update
  `docs/LSP-IDE-Support-Open-Questions.md` Q14 to "Resolved (client-side first; FuzzySharp contingency
  with two-tier ranking)" and add as-built notes to `docs/LSP-IDE-Support-Feature-Designs.md` §F7/§F8.
- PR 3 (F8 contingency, only if manual testing fails): add FuzzySharp package + `FuzzyCompletionMatcher`
  (two-tier ranking) + matcher tests + flip the DI registration.
