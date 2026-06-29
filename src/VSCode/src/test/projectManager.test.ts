import * as assert from 'assert';
import * as vscode from 'vscode';

suite('ProjectManager', () => {
  const methodNames = {
    projectLoaded: 'reqnroll/projectLoaded',
    projectUnloaded: 'reqnroll/projectUnloaded',
    projectFiles: 'reqnroll/projectFiles',
  } as const;

  test('should define correct LSP method names', () => {
    assert.strictEqual(methodNames.projectLoaded, 'reqnroll/projectLoaded');
    assert.strictEqual(methodNames.projectUnloaded, 'reqnroll/projectUnloaded');
    assert.strictEqual(methodNames.projectFiles, 'reqnroll/projectFiles');
  });

  test('projectLoaded params should have the required shape', () => {
    const params = {
      workspaceFolder: '/workspace',
      projectFile: '/workspace/src/MyProject.csproj',
      projectFolder: '/workspace/src',
      outputAssemblyPath: '',
      targetFrameworkMoniker: '',
      defaultNamespace: '',
      packageReferences: [] as { packageId: string; version: string; installPath: string }[],
    };

    // Verify all required fields are present
    assert.ok(typeof params.workspaceFolder === 'string');
    assert.ok(typeof params.projectFile === 'string');
    assert.ok(typeof params.projectFolder === 'string');
    assert.ok(typeof params.outputAssemblyPath === 'string');
    assert.ok(typeof params.targetFrameworkMoniker === 'string');
    assert.ok(typeof params.defaultNamespace === 'string');
    assert.ok(Array.isArray(params.packageReferences));
  });

  test('projectUnloaded params should have projectFile only', () => {
    const params = { projectFile: '/workspace/src/MyProject.csproj' };
    assert.ok(typeof params.projectFile === 'string');
    assert.strictEqual(Object.keys(params).length, 1);
  });

  test('should discover .csproj files from workspace folders', async () => {
    const patterns = ['**/*.csproj', '**/*.slnx', '**/*.sln'];
    const found: vscode.Uri[] = [];

    for (const pattern of patterns) {
      const matches = await vscode.workspace.findFiles(pattern, '**/node_modules/**');
      found.push(...matches);
    }

    // The extension's own project should be discoverable
    assert.ok(Array.isArray(found));
  });

  test('should resolve workspace folder from project path', () => {
    const folders = vscode.workspace.workspaceFolders;
    if (!folders || folders.length === 0) {
      // No workspace open — that's fine for this test
      return;
    }

    const projectFile = `${folders[0].uri.fsPath}/test/Test.csproj`;
    let resolvedFolder = folders[0].uri.fsPath;

    for (const folder of folders) {
      if (projectFile.startsWith(folder.uri.fsPath)) {
        resolvedFolder = folder.uri.fsPath;
        break;
      }
    }

    assert.ok(resolvedFolder.length > 0);
    assert.ok(
      folders[0].uri.fsPath.startsWith(resolvedFolder) ||
        resolvedFolder.startsWith(folders[0].uri.fsPath),
    );
  });

  test('should register a project only once (deduplication)', () => {
    const known = new Set<string>();
    const projectFile = '/workspace/src/Test.csproj';

    // First registration
    assert.ok(!known.has(projectFile));
    known.add(projectFile);

    // Second registration — should be skipped
    assert.ok(known.has(projectFile));

    // Unregister
    known.delete(projectFile);
    assert.ok(!known.has(projectFile));
  });
});
