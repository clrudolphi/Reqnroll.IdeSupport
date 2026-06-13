# Q23 — Session restore: LSP not activated for previously-open editors

**Status:** Plan drafted — not yet implemented
**Affects:** All VS.Extensibility features (code lens, commands, diagnostics, coloring) on session restore

---

## 1. Problem statement

When VS restores a previous editing session on solution load, previously-open editors do not receive
Reqnroll LSP services until each file is manually closed and reopened.  The symptom covers both file
types:

- **`.feature` files** — no syntax coloring, no diagnostics, no completion, no Go to Definition /
  Go to Hooks.
- **`.cs` files** — no step-usage code lens counts, "Find Step Usages" command appears but does
  nothing when clicked.

### Root cause: VS delayed document loading

When VS re-opens a session it creates **stub frames** — shallow placeholder entries in the Running
Document Table (RDT) — for every previously-open file.  A stub frame:

- does **not** initialize the text buffer
- does **not** run content-type classification
- does **not** send `textDocument/didOpen` to any LSP client

Documents remain as stubs until the user explicitly clicks a tab.  Only the **foreground** (active)
tab is fully initialized immediately; all background tabs stay as stubs.

See: [Delayed document loading](https://learn.microsoft.com/visualstudio/extensibility/internals/delayed-document-loading?view=visualstudio)

### Two distinct failure modes

| # | Scenario | What fails |
|---|---|---|
| **A** | Foreground editor on startup is a `.cs` file (or any non-`.feature` file) | `LanguageServerProvider` never activates — `CreateServerConnectionAsync` is never called.  LSP process does not start.  All features broken for the whole session until a `.feature` tab is clicked. |
| **B** | Foreground editor is a `.feature` file | LSP starts for that one file.  All other `.feature` and `.cs` background stubs never receive `textDocument/didOpen`.  Their per-file features (coloring, diagnostics, code lens) are absent until each tab is clicked. |

### Secondary issue: silent failure on command invoke

When the "Find Step Usages" or "Find Unused Step Definitions" command is invoked before the LSP is
running (`_state.Service == null`), both commands return silently.  The user receives no feedback.

### Secondary issue: extension host load timing (uncertain)

The Reqnroll VS.Extensibility extension is registered with `RequiresInProcessHosting = true`.  VS
reads the `extension.json` manifest at startup and is expected to load the extension host when any
contribution is first needed — including the "Reqnroll" menu contribution to the Extensions menu.

However, it is **not documented** whether VS.Extensibility guarantees extension-host loading from a
menu contribution alone when the dominant activation signal (the `LanguageServerProvider`) has never
fired.  If the extension host is gated on the LSP activation, the "Reqnroll" submenu would be absent
entirely in scenario A, not just non-functional.

This must be verified empirically (see §4.1).

---

## 2. Goals

1. LSP server starts on solution load even when no `.feature` file is the foreground editor.
2. All `.feature` stub frames receive `textDocument/didOpen` automatically after the LSP starts,
   without requiring the user to click each tab.
3. `.cs` editor shows step-usage code lens counts as soon as binding discovery (the out-of-proc
   Connector) completes, including when the `.cs` file is the foreground editor from session restore.
4. "Find Step Usages" and "Find Unused Step Definitions" show a user-visible message when invoked
   before the LSP is ready, rather than silently doing nothing.
5. All fixes gate correctly on `ClientIdeContext.IsVisualStudio` — no impact on non-VS clients.

---

## 3. Solution overview

Three cooperating pieces, each independently valuable:

```
Solution loads
  │
  ├── [Piece 1] ProvideAutoLoad fires ReqnrollPluginPackage.InitializeAsync
  │       └── OnAfterBackgroundSolutionLoadComplete
  │               ├── RDT scan: find .feature stub frames
  │               │       ├── found → force-init first stub → CreateServerConnectionAsync ← fixes A
  │               │       └── not found → open first .feature from project items ← fixes A (no stubs)
  │
  └── [Piece 2] OnServerInitializationResultAsync (already exists)
          ├── SendInitialProjectsAsync (already exists)
          ├── RDT scan: force-init remaining .feature stubs ← fixes B
          └── (server) workspace/codeLens/refresh after Connector run ← fixes .cs code lens
```

```
User clicks "Find Step Usages" before LSP ready
  └── [Piece 3] Show status-bar / InfoBar message ← fixes silent failure
```

---

## 4. Detailed design

### 4.1 Verify: does the extension host load without a `.feature` trigger?

Before implementing §4.2, run this manual test:

1. Open a solution that has `.feature` files and `.cs` binding files.
2. Close all editors so VS saves an empty editor session.
3. Reopen the solution; confirm no tabs are restored.
4. Check: does "Extensions › Reqnroll" appear in the menu?

- **Yes** → the extension host loads from the menu contribution alone.  Proceed with §4.2.
- **No** → the extension host is gated on the LSP.  The `ProvideAutoLoad` fix in §4.2 is even more
  critical; add explicit code in `ReqnrollPluginPackage.InitializeAsync` to force the VS.Extensibility
  host to activate (details TBD — may require calling into the `IVsExtensibilityPartner` service or
  touching a VS.Extensibility service from within the VSSDK package).

### 4.2 Piece 1 — `ProvideAutoLoad` + early `.feature` bootstrap

**File:** `ReqnrollPluginPackage.cs`

Add the `ProvideAutoLoad` attribute so the package loads immediately when a solution is present,
independent of which file is the foreground editor:

```csharp
[ProvideAutoLoad(
    UIContextGuids80.SolutionExists,
    PackageAutoLoadFlags.BackgroundLoad)]
[PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
[Guid(PackageGuidString)]
public sealed class ReqnrollPluginPackage : AsyncPackage
{
    protected override async Task InitializeAsync(
        CancellationToken cancellationToken,
        IProgress<ServiceProgressData> progress)
    {
        await base.InitializeAsync(cancellationToken, progress);

        // Wait for the solution to finish background loading before
        // the RDT contains meaningful document entries.
        await WaitForSolutionLoadAsync(cancellationToken);

        await EnsureFeatureFileActivatedAsync(cancellationToken);
    }
}
```

**`WaitForSolutionLoadAsync`** — subscribe to `IVsSolutionLoadEvents.OnAfterBackgroundSolutionLoadComplete`
or, if already loaded, return immediately.  Use `IVsSolution4` to check load state.

**`EnsureFeatureFileActivatedAsync`** — two-stage probe:

*Stage 1 — RDT scan (fast, zero side effects):*

Use `IVsRunningDocumentTable4` to enumerate all RDT cookies.  For each entry whose moniker ends in
`.feature`, check whether the frame is a stub (document data not yet initialized).  If found,
force-initialize the document data by calling `IVsWindowFrame.GetProperty(VSFPROPID_DocData)` on the
frame.

Accessing `VSFPROPID_DocData` on a stub frame triggers full document initialization — VS loads the
text buffer and attaches content-type classifiers — without changing the active tab.

*Stage 2 — Project scan fallback (if Stage 1 finds nothing):*

If no `.feature` stub frames exist in the RDT (i.e. no `.feature` file was open in the previous
session), enumerate `dte.Solution.Projects` and their `ProjectItems` to find any `.feature` file on
disk.  Call `dte.Documents.Open(path)` for the first file found, then close the document immediately
after (`Document.Close(vsSaveChanges.vsSaveChangesNo)`) — the open operation is enough to trigger
`CreateServerConnectionAsync`.  VS may leave the file open as an editor tab; acceptable side-effect
if it proves difficult to close without disruption.

**Verification point:** after `VSFPROPID_DocData` access, confirm that `textDocument/didOpen` is
observed in the LSP inspector log for that file.  If `VSFPROPID_DocData` alone does not trigger the
LSP pipeline (because the content-type event fires before our LSP client is registered), fall back to
`IVsWindowFrame.Show()` on the stub frame and restore the prior active frame afterwards.

### 4.3 Piece 2 — Force-init remaining stubs after LSP starts

**File:** `ReqnrollLanguageClient.cs`, at the end of `OnServerInitializationResultAsync`

After `SendInitialProjectsAsync` completes, scan the RDT again for any remaining `.feature` stub
frames and force-initialize them.  Extract the RDT logic into a shared helper
`VsStubFrameInitializer` (or a static method on `VsUtils`) so both Piece 1 and Piece 2 use the same
code path.

```csharp
// End of OnServerInitializationResultAsync, inside the try block after SendInitialProjectsAsync:
await VsStubFrameInitializer
    .ForceInitializeFeatureStubsAsync(serviceProvider, _traceSource, cancellationToken)
    .ConfigureAwait(false);
```

This handles scenario B (foreground was a `.feature` file; background tabs are still stubs).

### 4.4 Piece 2b — `workspace/codeLens/refresh` after binding discovery

**File:** LSP server — wherever the out-of-proc Connector run completes and populates the binding
registry.

After the binding registry is populated (i.e. after the Connector returns and step definitions are
indexed), the server must send:

```json
{ "method": "workspace/codeLens/refresh", "jsonrpc": "2.0" }
```

This causes VS to re-request code lens for all currently-open editors.  Without this, a `.cs` file
that was the foreground editor at startup will never receive code lens updates, even after the LSP
starts and the Connector finishes, because VS does not re-request code lens unless told to.

This is a server-side change only; no VS extension changes required.

**Prerequisite:** `textDocument/codeLens` must be declared in the server's `InitializeResult`
capabilities, and VS must support `workspace/codeLens/refresh` (it does — confirmed by the LSP
support table in the VS documentation).

### 4.5 Piece 3 — User-visible feedback when command is invoked before LSP is ready

**Files:** `FindStepUsagesCommand.cs`, `FindUnusedStepDefinitionsCommand.cs`

Replace the silent early-return with a status bar message:

```csharp
if (service is null || renderer is null)
{
    _fileLogger.LogWarning("FindStepUsagesCommand: LSP server not yet initialized.");
    await Extensibility.Shell().ShowStatusBarMessageAsync(
        "Reqnroll: LSP server not yet initialized — open a .feature file to activate it.",
        cancellationToken);
    return;
}
```

Use `VisualStudioExtensibility.Shell().ShowStatusBarMessageAsync` (available in VS.Extensibility).
If that API is not available in the current SDK version, use `IVsStatusbar` from the service provider
(requires marshalling to the UI thread).

---

## 5. Risks and mitigations

| Risk | Likelihood | Mitigation |
|---|---|---|
| `VSFPROPID_DocData` does not trigger `textDocument/didOpen` | Medium | Fall back to `IVsWindowFrame.Show()` + restore prior active frame |
| Opening a `.feature` file via `dte.Documents.Open` is disruptive (leaves an extra tab) | Low | Close the document immediately after; or find a lighter-weight trigger |
| `ProvideAutoLoad` increases VS startup time | Low | Use `BackgroundLoad` flag; do nothing if no `.feature` files found quickly |
| `workspace/codeLens/refresh` causes excessive VS churn on large solutions | Low | Only send after Connector run completes, not on every project-loaded event |
| Extension host load gated on LSP activation (§4.1 "No" path) | Unknown | Requires further investigation; fallback is to call into VS.Extensibility activation service from within the VSSDK package |

---

## 6. Implementation order

1. **Verify §4.1** — manual test, no code changes.
2. **Piece 3** (silent failure feedback) — small, safe, immediately valuable, independent.
3. **`ProvideAutoLoad` + `WaitForSolutionLoadAsync`** — scaffolding only, no behaviour yet.
4. **`EnsureFeatureFileActivatedAsync` Stage 1** (RDT stub scan).
5. **Verify** with LSP inspector log that `textDocument/didOpen` arrives.
6. **`EnsureFeatureFileActivatedAsync` Stage 2** (project scan fallback).
7. **Piece 2** (`OnServerInitializationResultAsync` stub flush).
8. **Piece 2b** (`workspace/codeLens/refresh` in server after Connector run).
9. **Regression test** — run through all four scenarios in §2 manually.

---

## 7. Out of scope

- JetBrains Rider integration — the session-restore mechanism differs.
- Making code lens appear instantaneously before the Connector run finishes — that requires
  incremental/streaming discovery, which is a separate feature.
- `.cs` file coloring or diagnostics from the LSP — the LSP is `reqnroll-gherkin` scoped; `.cs`
  services come from Roslyn and are unaffected.

---

## 8. Considered and rejected: Roslyn pre-warm of the foreground `.cs` file

**Proposal:** After the LSP starts but before the Connector finishes, invoke
`CSharpBindingDiscoveryService.UpdateFromSourceAsync` on the foreground `.cs` file to populate
partial binding data immediately, so code lens appears sooner for that file.

**Why rejected:**

1. **Architecture mismatch.**  `CSharpBindingDiscoveryService` requires
   `ConnectorBindingRegistryProvider` to exist in the project's properties (created synchronously by
   `OnProjectDiscovered`) but is designed as an *incremental patch* over a Connector-provided
   baseline — not as a standalone warm-up mechanism.  Running it against a
   `ProjectBindingRegistry.Invalid` baseline produces a registry containing only the bindings from
   that one file.

2. **Wrong counts.**  A registry built from a single file omits bindings inherited from base classes
   and step-definition libraries in other projects.  Code lens would display incorrect usage counts
   (typically under-counts), then snap to the correct value when the Connector finishes.  Transient
   wrong data is worse UX than temporarily absent data.

3. **No clean trigger.**  VS's LSP client will not re-send `textDocument/didOpen` for a `.cs` file
   that was already open when the LSP started.  Invoking the Roslyn path proactively would require a
   new custom notification (`reqnroll/warmupBindingFile`) or server-side disk reads — new surface
   area with non-trivial implementation cost.

4. **Marginal benefit.**  The Connector is already scheduled with a 500ms debounce immediately after
   `reqnroll/projectLoaded`.  The session-restore fix (§4.2) makes `reqnroll/projectLoaded` fire
   earlier, so the Connector starts earlier.  The pre-warm would save only the seconds between
   "Connector starts" and "Connector finishes" — a small window.

**Better alternative:** reduce the end-to-end gap by starting the LSP sooner (§4.2) and emitting
`workspace/codeLens/refresh` as soon as the Connector completes (§4.4), rather than introducing
partial state into the binding registry.
