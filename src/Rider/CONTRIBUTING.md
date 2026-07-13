# Reqnroll Rider Plugin

Kotlin-only IntelliJ Platform plugin that runs the Reqnroll.IdeSupport LSP server inside
Rider. It registers a `.feature` file type/language and an `LspServerSupportProvider`
(`src/main/kotlin/com/reqnroll/ide/rider/lsp`) — there is no ReSharper-SDK/.NET-backend
half; the IntelliJ Platform's built-in `com.intellij.platform.lsp.api` framework talks to
the server directly over stdio.

## First-time setup

The Gradle wrapper jar isn't committed (binary blob). Bootstrap it once, then use
`./gradlew` for everything after:

```
gradle wrapper --gradle-version 8.10
```

Inside the provided dev container (`.devcontainer/devcontainer.json`), a system `gradle`
is preinstalled for exactly this step.

## Build / run

```
./gradlew buildPlugin   # produces build/distributions/*.zip
./gradlew runIde         # launches a sandboxed Rider instance with the plugin loaded
```

## Bundling the LSP server

`ReqnrollServerPathResolver` expects the published server under
`server/<rid>/Reqnroll.IdeSupport.LSP.Server[.exe]` inside the plugin's own install
directory, for whichever RID matches the OS Rider is actually running on — so a real
distributable build needs every supported RID bundled at once, mirroring the layout
`src/VSCode`'s build produces (one universal, OS-detecting package).

There are two ways to populate `server/<rid>/`, both wired up in `build.gradle.kts`:

- **Local dev** — the `publishServer` task runs `dotnet publish` on
  `src/LSP/Reqnroll.IdeSupport.LSP.Server` for the host RID (override with
  `-PserverRid=<rid>`, e.g. `linux-x64`/`osx-arm64`) into `server/<rid>/` here
  (gitignored, like `src/VSCode/server`). `prepareSandbox` depends on it, so this is
  automatic:

  ```
  ./gradlew runIde                              # bundles the host RID only
  ./gradlew buildPlugin -PserverRid=linux-x64   # cross-publish a different single RID
  ```

  Requires the .NET SDK (`dotnet`) on `PATH` — the dev container does not currently
  include one; run these from a host that has it, or add the SDK to
  `docker/dev.Dockerfile` if you need `publishServer` inside the container.

- **CI** (see `.github/workflows/build-rider-plugin.yml`) — passes
  `-PlspServerBuildDir=<dir>`, where `<dir>` contains a `win-x64/`, `linux-x64/`,
  `osx-x64/`, `osx-arm64/` subdirectory (populated from the `server-<rid>` artifacts
  `test-lsp.yml` already built and tested). `publishServer` is skipped entirely in this
  mode — Gradle never needs `dotnet` on the CI runner — and `prepareSandbox` bundles
  every RID found under `<dir>` instead of just one.

## Manual verification (no local Rider install)

There is no locally installed Rider on the dev machine, and no plan to add one — this
is exactly what the dev container (`.devcontainer/devcontainer.json`) is for: it runs
Rider headless-from-the-container's-perspective, displayed on the host desktop over
WSLg's X11 forwarding (`DISPLAY=:0`). All verification below happens *inside* the
container, except publishing the server (the container has no .NET SDK — see the
"Local dev" bullet above — so publish from the Windows host, into the bind-mounted
repo, and point Gradle at it exactly the way CI does).

1. **Rebuild the container** in VS Code (`Dev Containers: Rebuild and Reopen in
   Container`) after any `docker/dev.Dockerfile` change. The image needs
   `libxext6`/`libxrender1`/`libxtst6`/`libxi6`/`libxrandr2` for the JetBrains
   Runtime's AWT/Swing toolkit to initialize at all — without them `runIde` fails
   immediately with `UnsatisfiedLinkError: libXext.so.6: cannot open shared object
   file` before a window ever appears. It also needs `libicu-dev`/`libssl-dev`/
   `zlib1g` for Rider's own .NET (CoreCLR) backend process — separate from anything
   our plugin does, Rider always spawns one alongside the JVM frontend — without
   which that backend SIGABRTs (`exit code 134`) right after the frontend loads.
2. **From the Windows host** (has `dotnet`), publish the server for `linux-x64` — that's
   the RID the container needs, since Rider itself runs inside it regardless of the
   host OS:
   ```
   dotnet restore src/LSP/Reqnroll.IdeSupport.LSP.Connector/Connector/Connector.csproj --runtime linux-x64
   dotnet publish src/LSP/Reqnroll.IdeSupport.LSP.Server/Reqnroll.IdeSupport.LSP.Server.csproj --configuration Release --runtime linux-x64 --self-contained true --output src/Rider/downloaded-server/linux-x64
   ```
3. **Inside the container**, the published binary needs its executable bit set — Windows
   bind mounts don't reliably preserve it:
   ```
   chmod +x src/Rider/downloaded-server/linux-x64/Reqnroll.IdeSupport.LSP.Server
   ```
4. **Inside the container**, bootstrap the Gradle wrapper once (see "First-time setup"
   above), then launch the sandbox using the CI-style external build dir, so Gradle
   never needs `dotnet`:
   ```
   cd src/Rider
   ./gradlew runIde -PlspServerBuildDir=$(pwd)/downloaded-server
   ```
5. First run downloads the Rider platform SDK (large, one-time). A sandboxed Rider
   window should eventually appear on the Windows desktop via WSLg.
6. Open/create a small scratch project containing a `.feature` file to trigger
   `ReqnrollLspServerSupportProvider.fileOpened`. Create it under
   `/workspaces/rider-samples` (a dedicated named volume, mounted in
   `devcontainer.json`) rather than anywhere under the repo checkout — it persists
   across container rebuilds the same way the repo does, but stays outside the git
   working tree entirely. Confirm the server actually started:
   - the LSP status widget / "Language Servers" view in Rider should list "Reqnroll";
   - `ps aux | grep Reqnroll.IdeSupport.LSP.Server` inside the container should show
     the process running;
   - the sandbox's `idea.log` (under `build/idea-sandbox/.../log/`) should show the LSP
     initialize handshake, with no errors from `ReqnrollServerPathResolver`.

Note the plugin currently only registers the file type and starts the server — there's
no client-side feature behavior yet (completions, diagnostics, etc. all come from the
server itself), so "success" here just means the process spawns and the LSP connection
initializes cleanly, not that anything visibly lights up in the editor.

## Testing (TODO — not yet written)

- `ReqnrollServerPathResolver` — plain JUnit, no platform fixture needed: RID selection
  for each `(os.name, os.arch)` combination, correct binary name per OS, and the error
  message when the binary is missing.
- `ReqnrollLspServerSupportProvider.fileOpened` — `BasePlatformTestCase` with a
  fake/spy `LspServerStarter`, asserting `ensureServerStarted` is (or isn't) called for
  `.feature`/`.cs` vs. other extensions.
- `ReqnrollLspServerDescriptor` — `isSupportedFile` gating, and `createCommandLine()`
  producing the right exe path plus `--ide rider --log-level Warning` args.
- File type/language registration — `BasePlatformTestCase` confirming a `.feature` file
  resolves to `ReqnrollFeatureFileType`/`ReqnrollFeatureLanguage` at runtime (catches
  `plugin.xml` wiring typos that `verifyPlugin` doesn't, since that only checks API
  compatibility).
- Deferred: a full end-to-end functional test (real Rider sandbox, open a `.feature`
  file, confirm the LSP connection comes up) — expensive, and there isn't much
  Rider-specific behavior to protect yet. Revisit once there's a PSI reference provider
  or similar genuinely Rider-side logic.
- Adding these requires `testFramework(TestFrameworkType.Platform)` in
  `build.gradle.kts`'s `dependencies { intellijPlatform { ... } }` block (not present
  yet) plus a `src/test/kotlin` source set.
