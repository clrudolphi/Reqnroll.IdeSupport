import * as path from 'path';
import * as vscode from 'vscode';
import { LanguageClient } from 'vscode-languageclient/node';

/**
 * Manages custom LSP notifications (reqnroll/projectLoaded, projectUnloaded)
 * for the VS Code extension.
 *
 * v1 approach: discovers `.csproj`/`.slnx`/`.sln` files in workspace folders
 * and sends projectLoaded with folder-prefix membership.  Output assembly
 * path, TFM, and package references are left empty (they require MSBuild
 * evaluation, which is v2 scope).  The server falls back to folder-prefix
 * file scanning when no projectFiles notification is received.
 */
export class ProjectManager {
  private readonly _client: LanguageClient;
  private readonly _watcher: vscode.FileSystemWatcher;
  private readonly _knownProjects = new Set<string>();
  private _disposables: vscode.Disposable[] = [];

  constructor(client: LanguageClient) {
    this._client = client;

    // Watch for project/solution file changes across all workspace folders
    this._watcher = vscode.workspace.createFileSystemWatcher('**/*.{csproj,slnx,sln}');

    this._disposables.push(
      this._watcher.onDidCreate((uri) => void this.onProjectCreated(uri)),
      this._watcher.onDidDelete((uri) => void this.onProjectDeleted(uri)),
    );

    // Discover any projects already present in the workspace
    void this.discoverExistingProjects();
  }

  /** Releases watcher and event subscriptions. */
  dispose(): void {
    this._watcher.dispose();
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

  // ── Notification sending ──────────────────────────────────────────────

  /**
   * Sends reqnroll/projectLoaded for the given project file.
   * Duplicate-safe: skips if already registered.
   */
  private async registerProject(uri: vscode.Uri): Promise<void> {
    const projectFile = uri.fsPath;
    if (this._knownProjects.has(projectFile)) return;

    const workspaceFolder = this.resolveWorkspaceFolder(projectFile);
    const projectFolder = path.dirname(projectFile);

    const params = {
      workspaceFolder,
      projectFile,
      projectFolder,
      // v1: these require MSBuild evaluation — leave empty for now
      outputAssemblyPath: '',
      targetFrameworkMoniker: '',
      defaultNamespace: '',
      packageReferences: [],
    };

    try {
      await this._client.sendNotification('reqnroll/projectLoaded', params);
      this._knownProjects.add(projectFile);
    } catch (err) {
      console.error(`ProjectManager: failed to send projectLoaded for ${projectFile}:`, err);
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
      await this._client.sendNotification('reqnroll/projectUnloaded', params);
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
