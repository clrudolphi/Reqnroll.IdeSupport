import * as assert from 'assert';
import * as vscode from 'vscode';

// Pull in all additional test suites so the single entry-point loads them all
import './projectManager.test';
import './lspInspectorLogger.test';
import './renameDisambiguation.test';
import './tableHighlightService.test';

/**
 * Waits for the extension's language client to reach the running state by polling
 * for a Reqnroll-specific command that the extension registers only after
 * `client.start()` completes successfully. Avoids fragile hardcoded sleeps.
 */
async function waitForClientRunning(timeoutMs = 10_000, pollIntervalMs = 200): Promise<void> {
  const deadline = Date.now() + timeoutMs;
  while (Date.now() < deadline) {
    try {
      const commands = await vscode.commands.getCommands(true);
      if (commands.includes('reqnroll.findStepUsages')) {
        return;
      }
    } catch {
      // poll cycle error — retry
    }
    await new Promise((resolve) => setTimeout(resolve, pollIntervalMs));
  }
  const commands = await vscode.commands.getCommands(true);
  throw new Error(
    `Language client did not start within ${timeoutMs}ms. ` +
      `Registered commands: ${commands.filter((c) => c.startsWith('reqnroll')).join(', ') || '(none)'}`,
  );
}

suite('Reqnroll Extension Tests', () => {
  const extensionId = 'reqnroll.reqnroll-ide-support';

  test('Extension should be present', () => {
    const ext = vscode.extensions.getExtension(extensionId);
    assert.ok(ext, `Extension ${extensionId} is not installed`);
  });

  test('Extension should activate on gherkin language', async () => {
    const ext = vscode.extensions.getExtension(extensionId)!;
    await ext.activate();
    assert.ok(ext.isActive, 'Extension did not activate');
  });

  test('Language client should start after activation', async () => {
    const ext = vscode.extensions.getExtension(extensionId)!;
    await ext.activate();
    assert.ok(ext.isActive, 'Extension should be active after activation');

    // Wait for the language client to fully start (poll for a Reqnroll command
    // that's registered only after client.start().then() completes).
    await waitForClientRunning();
  });

  test('Gherkin language should be registered', async () => {
    const languages = await vscode.languages.getLanguages();
    assert.ok(
      languages.includes('gherkin'),
      `gherkin language not registered. Available: ${languages.join(', ')}`,
    );
  });
});
