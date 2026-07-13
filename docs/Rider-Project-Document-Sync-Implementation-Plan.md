# Rider Client-Side Project & Document Lifecycle Sync — Implementation Plan

> **Status:** Design only — not started.
> **Audience:** Core team contributors picking up Rider glue work after the skeleton (`src/Rider`).
> **Supersedes:** Risk **R1 · Rider Custom Notification Transport** in
> [Porting-to-VSCode-Rider-Analysis.md](Porting-to-VSCode-Rider-Analysis.md) — that risk asked
> "investigate whether Rider's `LspServer` API supports `sendNotification`"; this plan answers it
> (short answer: no generic method, but a supported typed-interface path exists — §3.1) and scopes
> the actual work.
> **Prior art referenced throughout:** `src/VisualStudio/.../LspNotifications/VsProjectEventMonitor.cs`
> + `VsProjectPayloadBuilder.cs` + `LspProjectPreloadPusher.cs` (VS), `src/VSCode/src/projectManager.ts`
> (VS Code).

---

## 1. Why this exists

The server defines four "client pushes lifecycle state" custom notifications
(`src/LSP/Reqnroll.IdeSupport.LSP.Server/Protocol/LspMethodNames.cs`):

| Method | Purpose |
|---|---|
| `reqnroll/projectLoaded` | Project build properties (output assembly, TFM, package refs, default namespace) — needed for reflection-based binding discovery. |
| `reqnroll/projectUnloaded` | Signals project removal. |
| `reqnroll/projectFiles` | Authoritative file-membership index (which `.feature`/`.cs` files belong to which project), baseline + incremental deltas — needed for linked/excluded files the folder-prefix fallback can't handle. |
| `reqnroll/documentActivated` | Tab-activation signal (issue #85) — the server can't infer "this is now the visible tab" from `didOpen`/`didClose` alone; without it, stale diagnostics/semantic tokens can persist on a reactivated tab. |

VS and VS Code both implement client-side glue that watches project/file/tab lifecycle events and
sends these four notifications. Without it, Rider falls back to whatever degraded behavior the
server has for absent project data (folder-prefix membership, no reflection-based discovery,
stale-tab diagnostics) — the same fallback VS Code relies on today. This plan scopes building the
Rider equivalent.

**Out of scope:** the request-style custom methods (`reqnroll/findStepUsages`,
`reqnroll/goToStepDefinitions`, `reqnroll/goToHooks`, `reqnroll/findUnusedStepDefinitions`,
`reqnroll/renameTargets`, `reqnroll/selectRenameTarget`, `reqnroll/documentSymbolHierarchical`).
Those are user-action-driven (a context-menu item or explicit command), not lifecycle-driven, and
need their own UI-action wiring (Rider `AnAction`s) — a separate, later plan.

---

## 2. What we already confirmed (no further research needed)

- **No generic send-anything API.** `com.intellij.platform.lsp.api.LspServer`/`LspServerManager`
  expose no `sendNotification(method, params)`. Confirmed against the platform source
  (`platform/lsp/src/api/LspServer.kt`, `LspServerManager.kt`).
- **Typed custom-notification path exists and is documented.** Per JetBrains' own LSP docs: override
  `LspServerDescriptor.lsp4jServerClass` with a custom interface extending LSP4J's base
  `LanguageServer`, annotating additional methods with `@JsonNotification`/`@JsonRequest`. The
  platform hands back a typed proxy for that interface. This is the mechanism §3.1 designs against.
- **No pipe/middleware interception** (established in prior work on this plugin) — so unlike VS's
  `LspInterceptingPipe`, this can't be layered in as a message-stream interceptor. It has to be
  independent Kotlin listener code calling the typed proxy directly, parallel to (not wrapping)
  whatever the platform's generic document-sync already does.
- **Wire format is camelCase** (`CamelCasePropertyNamesContractResolver`,
  `LanguageServerOptionsExtensions.cs:45`) — confirmed so the Kotlin DTOs below use exact matching
  property names.

---

## 3. Design

### 3.1 Transport: a custom LSP4J client interface

```kotlin
// src/main/kotlin/com/reqnroll/ide/rider/lsp/protocol/ReqnrollLanguageServer.kt
interface ReqnrollLanguageServer : LanguageServer {
    @JsonNotification("reqnroll/projectLoaded")
    fun projectLoaded(params: ReqnrollProjectLoadedParams)

    @JsonNotification("reqnroll/projectUnloaded")
    fun projectUnloaded(params: ReqnrollProjectUnloadedParams)

    @JsonNotification("reqnroll/projectFiles")
    fun projectFiles(params: ReqnrollProjectFilesParams)

    @JsonNotification("reqnroll/documentActivated")
    fun documentActivated(params: DocumentActivatedParams)
}
```

`ReqnrollLspServerDescriptor` overrides `lsp4jServerClass` to return
`ReqnrollLanguageServer::class.java` instead of the platform default. Once the server connects, the
descriptor (or a service holding a reference to it) exposes the typed proxy so the event-watcher
classes in §3.3 can call `server.projectLoaded(...)` etc. directly — no JSON string-building by
hand, unlike VS's `JsonConvert.SerializeObject(paramsObj, ...)` + raw pipe write (that pattern
exists there only because VS intercepts at the raw-pipe level; here LSP4J's proxy handles
serialization for us, which is strictly less code).

**Open question:** exactly how a Kotlin class obtains the live proxy instance for an
already-`ensureServerStarted`-ed descriptor isn't yet confirmed against 2024.3's exact API surface
(candidates: a property on `LspServer`, or storing the proxy from a `createLsp4jClient()`-adjacent
callback). Resolve this in Phase 0 (§5) before building anything on top of it — it's the load-bearing
fact the rest of the design depends on.

### 3.2 Payload mapping

Kotlin data classes mirroring the server's DTOs exactly (camelCase, matching
`src/LSP/Reqnroll.IdeSupport.LSP.Server/Protocol/*.cs` and
`Features/DocumentActivated/DocumentActivatedParams.cs` field-for-field):

| Server DTO | Rider-sourced fields | Source (§3.3) |
|---|---|---|
| `ReqnrollProjectLoadedParams` | `workspaceFolder`, `projectFile`, `projectFolder`, `outputAssemblyPath`, `targetFrameworkMoniker`, `defaultNamespace`, `packageReferences[]` | Rider project model — biggest unknown, see §3.3/§6 Phase 0 |
| `ReqnrollProjectUnloadedParams` | `projectFile` | Workspace model listener |
| `ReqnrollProjectFilesParams` | `projectFile`, `targetFrameworkMoniker`, `kind` (baseline/delta), `files[]` (`path`, `role`, `added`) | Workspace model + `VirtualFileListener`/`AsyncFileListener` |
| `DocumentActivatedParams` | `uri` | `FileEditorManagerListener` |

`role` classification (`Feature` vs `Binding`) is a pure extension check — port
`VsProjectEventMonitor.ClassifyRole` verbatim (`.feature` → `Feature`, `.cs` → `Binding`, else
untracked).

### 3.3 Event sources — mapping VS's event table to Rider/IntelliJ Platform APIs

VS's `VsProjectEventMonitor` subscribes to: `SolutionEvents` (opened/closed, project added/removed),
`BuildEvents` (build done → full resend), `WindowEvents` (tab activation), and
`IVsTrackProjectDocumentsEvents2` (per-file add/remove/rename — needed because Solution Explorer
single-file operations don't raise solution/build events at all, issue #32). Rider equivalents:

| VS source | Fires on | Confirmed Rider/IntelliJ equivalent |
|---|---|---|
| `SolutionEvents.Opened`/`AfterClosing` | Solution open/close | `ProjectManagerListener` (generic IntelliJ Platform, not Rider-specific) — confirmed to exist. |
| `SolutionEvents.ProjectAdded`/`ProjectRemoved` | Project add/remove | IntelliJ's `WorkspaceModel`/`ModuleListener` — generic IntelliJ Platform API, exists. Whether it carries enough .NET-specific data (TFM, package refs) is the open question, not whether the add/remove event itself exists. |
| `BuildEvents.OnBuildDone` | Any build/rebuild completes | **Unconfirmed for Rider.** Candidates: a Rider-specific build-event listener (if the .NET backend exposes one to the frontend), or fall back to VS Code's approach — a file watcher on `**/bin/**/*.dll` (`VirtualFileManager.addAsyncFileListener`) as a proxy for "a build happened." Needs Phase 0 research. |
| `IVsTrackProjectDocumentsEvents2` (add/remove/rename) | Single-file Solution Explorer ops | `VirtualFileListener`/`AsyncFileListener` (generic IntelliJ Platform, `VirtualFileManager.addAsyncFileListener`) — confirmed to exist and fire for individual file add/remove/rename, matching the "no solution/build event" gap issue #32 called out. |
| `WindowEvents.WindowActivated` (filtered to `.feature` docs) | Tab switch | `FileEditorManagerListener.selectionChanged` — confirmed to exist, standard IntelliJ Platform editor-tab API. Directly analogous; likely simpler than VS's DTE `Window`/`Document` COM traversal. |

**The one real unknown is `ReqnrollProjectLoadedParams`'s rich fields** (`outputAssemblyPath`,
`targetFrameworkMoniker`, `defaultNamespace`, `packageReferences`). VS gets these from a real
Visual Studio Project System; VS Code has none and shells out to `dotnet msbuild` for evaluation
(`msbuildEvaluator.ts`). Rider's actual .NET project model lives in its .NET backend process (the
same "ReSharperHost" process that needed `libicu`/`libssl` to start under the dev container — see
`src/Rider/CONTRIBUTING.md`'s manual-verification section) — whether that data is already reflected into something
the Kotlin/JVM frontend can read directly (Rider's frontend project-view/module bridge), or whether
it requires defining custom RD protocol models (the `protocol/` Gradle submodule pattern seen in
Thomas Heijtink's sample plugin, not present in our skeleton) to pull backend-only data across, is
**not yet confirmed** and is this plan's biggest risk (§7 R1).

### 3.4 Reuse from `VsProjectEventMonitor`/`VsProjectPayloadBuilder`

Port verbatim (logic is IDE-agnostic):
- `ClassifyRole` (extension → `ProjectFileRole`)
- The delta-vs-baseline decision structure (`ProjectFilesKind`)
- `DocumentActivationState`'s state machine for issue #85 (avoid resending `documentActivated` for
  a tab that's already active/hasn't changed) — port the state machine, not the DTE-specific
  `Window`/`Document` plumbing around it.

---

## 4. Non-goals

- Not replicating VS's pipe-level `LspInspectorLogger`/interceptor architecture — established
  separately as impossible on Rider (no interception hook).
- Not building the request-style custom methods (§1 "Out of scope").
- Not attempting cross-project linked-file resolution beyond what `ReqnrollProjectFilesParams`
  already models — that's a server-side concern, unaffected by which client sends the notification.

---

## 5. Phased implementation plan

**Phase 0 — Research spike (do first, blocks everything else):**
1. Confirm how to obtain the live `ReqnrollLanguageServer` proxy from a running
   `ReqnrollLspServerDescriptor` instance (§3.1 open question).
2. Confirm what Rider's frontend project model actually exposes for a .NET project — specifically
   whether TFM/package-references/output-assembly-path are already bridged into IntelliJ's generic
   `Module`/`WorkspaceModel` for Rider projects, or whether accessing them requires the RD protocol
   (custom `protocol/` submodule + generated model classes). This single finding determines whether
   Phase 2 is "read some existing API" (small) or "define + wire an RD protocol model" (large,
   comparable in size to `ImplicitReferenceProvider` from the original porting estimate).
3. Confirm whether Rider exposes a build-completion event to the frontend, or whether the
   `**/bin/**/*.dll` file-watcher fallback (VS Code's approach) is necessary.

Do not start Phase 1+ until these three are answered — they change the shape/size of the rest of
this plan.

**Phase 1 — Transport plumbing** (small, ~50-70 lines Kotlin, matches the original porting
analysis's R5 "~70 lines" estimate for this piece specifically):
- `ReqnrollLanguageServer` interface (§3.1).
- Wire it into `ReqnrollLspServerDescriptor` via `lsp4jServerClass`.
- Kotlin DTOs mirroring §3.2's table.
- A thin "send if connected, log+swallow if not" wrapper (mirrors VS's `TrySend*Async` try/catch
  pattern) using `ReqnrollDebugLogger` for failures.

**Phase 2 — Project lifecycle** (size depends entirely on Phase 0 finding #2):
- `ModuleListener`/`WorkspaceModel` subscription → `projectLoaded`/`projectUnloaded`.
- Initial flush on project open (mirrors `SendInitialProjectsAsync`).
- Full resend on build-completion signal (Phase 0 finding #3 determines the mechanism).

**Phase 3 — File membership** (`reqnroll/projectFiles`):
- `AsyncFileListener` for individual add/remove/rename (the issue #32 gap VS specifically had to
  add `IVsTrackProjectDocumentsEvents2` for) → deltas.
- Baseline send alongside each `projectLoaded` (mirrors `TrySendProjectFilesAsync` being called
  right after `TrySendProjectLoadedAsync`).

**Phase 4 — Document activation** (`reqnroll/documentActivated`):
- `FileEditorManagerListener.selectionChanged`, filtered to `.feature` files.
- Port `DocumentActivationState`'s dedup logic verbatim.

---

## 6. Testing

Extends the existing TODO list in `src/Rider/CONTRIBUTING.md`:
- Phase 1 DTOs/interface: plain JUnit round-trip serialization tests (Kotlin object → JSON →
  compare against a fixture captured from the equivalent VS/VS Code payload, to catch camelCase or
  field-name drift early).
- Phases 2-4 listeners: `BasePlatformTestCase`-based tests using IntelliJ's in-memory test project
  fixtures, asserting the right notification fires (via a fake `ReqnrollLanguageServer` proxy
  recording calls) for project open/close, file add/remove/rename, and tab switch — analogous to
  the already-planned `fileOpened` gating test.
- Explicitly deferred (per the existing CONTRIBUTING.md note): a real end-to-end test against the
  actual server, confirming e.g. `projectFiles` absence really does degrade to folder-prefix
  membership as expected — do this once Phase 0-4 land and there's something concrete to break.

---

## 7. Risks

| ID | Risk | Mitigation |
|---|---|---|
| **R1** | Phase 0 finding #2 (Rider project-model data access) turns out to require the RD protocol — substantially larger effort than the "~70 lines" original estimate assumed. | Time-box the Phase 0 spike; if RD protocol is required, treat it as its own follow-up plan sized like `ImplicitReferenceProvider` (~150 lines) rather than folding it into this one. |
| **R2** | No confirmed build-completion event on Rider's frontend (Phase 0 finding #3). | Fall back to VS Code's `**/bin/**/*.dll` watcher approach — already a proven, if coarser, signal. |
| **R3** | `lsp4jServerClass`/typed-proxy mechanism is under-documented (only one doc sentence found, no code sample retrieved) — actual usage pattern may differ from §3.1's sketch once tried. | Phase 0 spike also includes a throwaway "send one custom notification and confirm the server receives it" smoke test before committing to the full design. |
| **R4** | Degraded-but-functional fallback (folder-prefix membership, no reflection discovery) may already be "good enough" for common cases, making this lower priority than it looks. | Confirm via manual testing in the sample project (`/workspaces/rider-samples`) whether symptoms are actually visible before investing Phase 1-4 effort. |

---

## References

- [Porting-to-VSCode-Rider-Analysis.md](Porting-to-VSCode-Rider-Analysis.md) §5.3, §6 R1 (superseded by this plan)
- [VSCode-Extension-Implementation-Plan.md](VSCode-Extension-Implementation-Plan.md) — Rider (Phase 2) task table, R5
- `src/VisualStudio/Reqnroll.IdeSupport.VisualStudio.Extension/LspNotifications/VsProjectEventMonitor.cs`,
  `VsProjectPayloadBuilder.cs`, `LspProjectPreloadPusher.cs`
- `src/VSCode/src/projectManager.ts`, `src/VSCode/src/msbuildEvaluator.ts`
- `src/LSP/Reqnroll.IdeSupport.LSP.Server/Protocol/LspMethodNames.cs`,
  `ReqnrollProjectLoadedParams.cs`, `ReqnrollProjectFilesParams.cs`, `ReqnrollProjectUnloadedParams.cs`,
  `Features/DocumentActivated/DocumentActivatedParams.cs`
- `src/Rider/CONTRIBUTING.md` (Testing TODO, Logging sections this plan extends)
