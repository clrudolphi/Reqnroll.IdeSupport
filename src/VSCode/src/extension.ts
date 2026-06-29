import * as fs from 'fs';
import * as path from 'path';
import * as vscode from 'vscode';
import { LanguageClient, LanguageClientOptions, ServerOptions } from 'vscode-languageclient/node';

let client: LanguageClient | undefined;

/**
 * Resolves the path to the Reqnroll LSP server binary.
 *
 * In development (VSIX not yet built), the server is located relative to
 * this source directory's build output. In production (packaged .vsix),
 * the server is bundled inside the extension under `server/<rid>/`.
 */
function resolveServerPath(context: vscode.ExtensionContext): string {
  const isProduction = context.extensionMode === vscode.ExtensionMode.Production;

  if (isProduction) {
    // Bundled inside the .vsix under server/<rid>/
    const rid =
      process.platform === 'win32'
        ? 'win-x64'
        : process.platform === 'darwin'
          ? process.arch === 'arm64'
            ? 'osx-arm64'
            : 'osx-x64'
          : 'linux-x64';
    const serverDir = path.join(context.extensionPath, 'server', rid);
    // Windows has .exe extension, macOS/Linux do not
    const binaryName =
      process.platform === 'win32'
        ? 'Reqnroll.IdeSupport.LSP.Server.exe'
        : 'Reqnroll.IdeSupport.LSP.Server';
    const candidate = path.join(serverDir, binaryName);
    if (fs.existsSync(candidate)) {
      return candidate;
    }
    // Fallback: try without RID subfolder (legacy layout)
    const legacy = path.join(context.extensionPath, 'server', binaryName);
    if (fs.existsSync(legacy)) {
      return legacy;
    }
    throw new Error(
      `Reqnroll LSP server not found at ${candidate} or ${legacy}. ` +
        'Ensure the server is published (see scripts/publish-server.sh).',
    );
  }

  // Development: resolve relative to the repository root
  return path.join(
    context.extensionPath,
    '..',
    '..',
    'src',
    'LSP',
    'Reqnroll.IdeSupport.LSP.Server',
    'bin',
    'Release',
    'net10.0',
    'win-x64',
    'publish',
    'Reqnroll.IdeSupport.LSP.Server.exe',
  );
}

export function activate(context: vscode.ExtensionContext): void {
  const serverPath = resolveServerPath(context);

  const serverOptions: ServerOptions = {
    command: serverPath,
    args: ['--ide', 'vscode'],
    options: {
      env: { ...process.env },
    },
  };

  const clientOptions: LanguageClientOptions = {
    documentSelector: [{ language: 'gherkin' }, { language: 'csharp', pattern: '**/*.cs' }],
    synchronize: {
      fileEvents: vscode.workspace.createFileSystemWatcher('**/*.{feature,cs}'),
    },
  };

  client = new LanguageClient(
    'reqnroll-lsp',
    'Reqnroll Language Server',
    serverOptions,
    clientOptions,
  );

  void client.start();
}

export function deactivate(): Thenable<void> | undefined {
  return client?.stop();
}
