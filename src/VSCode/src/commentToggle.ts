import * as vscode from 'vscode';
import { LanguageClient } from 'vscode-languageclient/node';
import { normalizeSelectionLines } from './selectionUtils';

export async function doToggleComment(client: LanguageClient): Promise<void> {
  const editor = vscode.window.activeTextEditor;
  if (!editor) return;

  const sel = editor.selection;
  const [startLine, endLine] = normalizeSelectionLines(
    sel.start.line,
    sel.end.line,
    sel.end.character,
  );

  try {
    await client.sendRequest('workspace/executeCommand', {
      command: 'reqnroll.toggleComment',
      arguments: [editor.document.uri.toString(), startLine, endLine],
    });
  } catch (err: unknown) {
    const msg = err instanceof Error ? err.message : String(err);
    void vscode.window.showErrorMessage(`Reqnroll: Toggle Comment failed — ${msg}`);
  }
}
