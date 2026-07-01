import * as path from 'path';
import * as vscode from 'vscode';
import { LanguageClient } from 'vscode-languageclient/node';
import { evaluateProject, ProjectFileItem } from './msbuildEvaluator';
import { ReqnrollMethods } from './lspMethods';

// Mirrors ProjectFilesKind / ProjectFileRole in
// src/LSP/Reqnroll.IdeSupport.LSP.Server/Protocol/ReqnrollProjectFilesParams.cs
const enum ProjectFilesKind {
  Baseline = 0,
  Delta = 1,
}
const enum ProjectFileRole {
  Feature = 0,
  Binding = 1,
}

// A burst of file-system events (e.g. a git checkout, a template extraction) collapses into a
// single re-evaluation per project rather than one `dotnet msbuild` run per event.
const RESEND_DEBOUNCE_MS = 500;

/**
 * Manages custom LSP notifications (reqnroll/projectLoaded, projectUnloaded, projectFiles)
 * for the VS Code extension.
 *
 * v1: sends projectLoaded with empty assembly/TFM/package fields — server
 *     falls back to folder-prefix file scanning.
 * v2: runs `dotnet msbuild` to populate OutputAssemblyPath, TFM, and
 *     package references, enabling reflection-based binding discovery.
 * v3: also sends reqnroll/projectFiles (baseline) so the server's per-file membership index
 *     (see LspWorkspaceScopeManager / MembershipState.cs) can leave every file `Pending` and
 *     folder-prefix-fallback state, matching what VS's VsProjectEventMonitor already provides.
 *     Without this, linked files outside a project's folder and files excluded via .csproj
 *     globs are handled by best-effort folder-prefix matching indefinitely.
 */
export class ProjectManager {
  private readonly _client: LanguageClient;
  private readonly _watcher: vscode.FileSystemWatcher;
  private readonly _fileWatcher: vscode.FileSystemWatcher;
  private readonly _knownProjects = new Set<string>();
  private readonly _resendTimers = new Map<string, ReturnType<typeof setTimeout>>();
  private _disposables: vscode.Disposable[] = [];

  constructor(client: LanguageClient) {
    this._client = client;

    // Watch for project/solution file changes across all workspace folders
    this._watcher = vscode.workspace.createFileSystemWatcher('**/*.{csproj,slnx,sln}');

    this._disposables.push(
      this._watcher.onDidCreate((uri) => void this.onProjectCreated(uri)),
      this._watcher.onDidDelete((uri) => void this.onProjectDeleted(uri)),
    );

    // Re-evaluate a project's file membership when a .cs/.feature file is added or removed
    // under it, so the server's membership index doesn't go stale after the initial baseline
    // (e.g. a new step-definition file, or a deleted/renamed .feature file).
    this._fileWatcher = vscode.workspace.createFileSystemWatcher('**/*.{cs,feature}');

    this._disposables.push(
      this._fileWatcher.onDidCreate((uri) => this.scheduleResend(uri)),
      this._fileWatcher.onDidDelete((uri) => this.scheduleResend(uri)),
    );

    // Discover any projects already present in the workspace
    void this.discoverExistingProjects();
  }

  /** Releases watchers, pending timers, and event subscriptions. */
  dispose(): void {
    this._watcher.dispose();
    this._fileWatcher.dispose();
    for (const timer of this._resendTimers.values()) clearTimeout(timer);
    this._resendTimers.clear();
    for (const d of this._disposables) d.dispose();
    this._disposables = [];
    this._knownProjects.clear();
  }

  // ── Discovery ─────────────────────────────────────────────────────────

  /**
   * Scans all workspace folders for .csproj files and registers them.
   */
  private async discoverExistingProjects(): Promise<void> {
    const patterns = ['**/*.csproj', '**/*.slnx', '**/*.sln'];
    const uris = new Set<string>();

    for (const pattern of patterns) {
      const matches = await vscode.workspace.findFiles(pattern, '**/node_modules/**');
      for (const uri of matches) {
        uris.add(uri.toString());
      }
    }

    for (const uriStr of uris) {
      const uri = vscode.Uri.parse(uriStr);
      await this.registerProject(uri);
    }
  }

  // ── Event handlers ────────────────────────────────────────────────────

  private async onProjectCreated(uri: vscode.Uri): Promise<void> {
    await this.registerProject(uri);
  }

  private async onProjectDeleted(uri: vscode.Uri): Promise<void> {
    await this.unregisterProject(uri);
  }

  /**
   * Debounces a full membership re-evaluation for the project (if any) that owns `uri`.
   * No-op for files under a project VS Code hasn't discovered yet — that project's own
   * registration will send a baseline that already includes the file.
   */
  private scheduleResend(uri: vscode.Uri): void {
    const projectFile = this.findOwningProject(uri.fsPath);
    if (!projectFile) return;

    const existing = this._resendTimers.get(projectFile);
    if (existing) clearTimeout(existing);

    this._resendTimers.set(
      projectFile,
      setTimeout(() => {
        this._resendTimers.delete(projectFile);
        void this.resendProjectFiles(projectFile);
      }, RESEND_DEBOUNCE_MS),
    );
  }

  /** Nearest known .csproj whose directory contains `filePath` (deepest match wins). */
  private findOwningProject(filePath: string): string | undefined {
    let best: string | undefined;
    let bestLen = 0;
    for (const projectFile of this._knownProjects) {
      if (!projectFile.endsWith('.csproj')) continue;
      const folder = path.dirname(projectFile) + path.sep;
      if (filePath.toLowerCase().startsWith(folder.toLowerCase()) && folder.length > bestLen) {
        best = projectFile;
        bestLen = folder.length;
      }
    }
    return best;
  }

  /** Re-runs MSBuild evaluation for an already-registered project and resends its baseline. */
  private async resendProjectFiles(projectFile: string): Promise<void> {
    const props = await evaluateProject(projectFile);
    if (!props) return; // msbuild unavailable — index stays Pending, same as v1 fallback
    await this.sendProjectFilesBaseline(projectFile, props.targetFrameworkMoniker, props.files);
  }

  // ── Notification sending ──────────────────────────────────────────────

  /**
   * Sends reqnroll/projectLoaded (and, when MSBuild evaluation succeeds, reqnroll/projectFiles)
   * for the given project file. Falls back to empty projectLoaded fields and no projectFiles
   * baseline when msbuild is unavailable (v1 compat — the file stays folder-prefix `Pending`).
   * Duplicate-safe: skips if already registered.
   */
  private async registerProject(uri: vscode.Uri): Promise<void> {
    const projectFile = uri.fsPath;
    if (this._knownProjects.has(projectFile)) return;

    // Only .csproj files get MSBuild evaluation; .slnx/.sln just mark as known
    if (!projectFile.endsWith('.csproj')) {
      this._knownProjects.add(projectFile);
      return;
    }

    const workspaceFolder = this.resolveWorkspaceFolder(projectFile);
    const projectFolder = path.dirname(projectFile);

    // v2: evaluate via dotnet msbuild
    const props = await evaluateProject(projectFile);

    const params = {
      workspaceFolder,
      projectFile,
      projectFolder,
      outputAssemblyPath: props?.outputAssemblyPath ?? '',
      targetFrameworkMoniker: props?.targetFrameworkMoniker ?? '',
      defaultNamespace: props?.defaultNamespace ?? '',
      packageReferences:
        props?.packageReferences.map((r) => ({
          packageId: r.packageId,
          version: r.version,
          installPath: '',
        })) ?? [],
    };

    try {
      await this._client.sendNotification(ReqnrollMethods.projectLoaded, params);
      this._knownProjects.add(projectFile);
    } catch (err) {
      console.error(`ProjectManager: failed to send projectLoaded for ${projectFile}:`, err);
      return;
    }

    // v3: populate the server's per-file membership index, same data VS's
    // VsProjectEventMonitor.SendInitialProjectsAsync sends via TrySendProjectFilesAsync.
    if (props) {
      await this.sendProjectFilesBaseline(projectFile, props.targetFrameworkMoniker, props.files);
    }
  }

  /** Sends a reqnroll/projectFiles baseline (full snapshot) for one project. */
  private async sendProjectFilesBaseline(
    projectFile: string,
    targetFrameworkMoniker: string,
    files: readonly ProjectFileItem[],
  ): Promise<void> {
    const params = {
      projectFile,
      targetFrameworkMoniker,
      kind: ProjectFilesKind.Baseline,
      files: files.map((f) => ({
        path: f.path,
        role: f.role === 'feature' ? ProjectFileRole.Feature : ProjectFileRole.Binding,
        added: true,
      })),
    };

    try {
      await this._client.sendNotification(ReqnrollMethods.projectFiles, params);
    } catch (err) {
      console.error(`ProjectManager: failed to send projectFiles for ${projectFile}:`, err);
    }
  }

  /**
   * Sends reqnroll/projectUnloaded for the given project file.
   * No-op if not in the known set.
   */
  private async unregisterProject(uri: vscode.Uri): Promise<void> {
    const projectFile = uri.fsPath;
    if (!this._knownProjects.has(projectFile)) return;

    const params = { projectFile };

    try {
      await this._client.sendNotification(ReqnrollMethods.projectUnloaded, params);
      this._knownProjects.delete(projectFile);
    } catch (err) {
      console.error(`ProjectManager: failed to send projectUnloaded for ${projectFile}:`, err);
    }
  }

  // ── Helpers ───────────────────────────────────────────────────────────

  /**
   * Finds the workspace folder that contains the given project file path.
   * Falls back to the first workspace folder.
   */
  private resolveWorkspaceFolder(projectFile: string): string {
    const folders = vscode.workspace.workspaceFolders;
    if (!folders || folders.length === 0) return projectFile;

    for (const folder of folders) {
      if (projectFile.startsWith(folder.uri.fsPath)) {
        return folder.uri.fsPath;
      }
    }

    return folders[0].uri.fsPath;
  }
}
