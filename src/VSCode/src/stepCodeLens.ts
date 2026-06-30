import * as vscode from 'vscode';
import { LanguageClient } from 'vscode-languageclient/node';

interface LspPosition {
  line: number;
  character: number;
}

interface LspCodeLens {
  range: { start: LspPosition; end: LspPosition };
  command?: { title: string; command: string; arguments?: unknown[] };
}

export function registerStepCodeLens(
  client: LanguageClient,
  context: vscode.ExtensionContext,
): void {
  const provider: vscode.CodeLensProvider = {
    async provideCodeLenses(document: vscode.TextDocument): Promise<vscode.CodeLens[]> {
      try {
        const lenses = await client.sendRequest<LspCodeLens[] | null>('textDocument/codeLens', {
          textDocument: { uri: document.uri.toString() },
        });
        if (!lenses || lenses.length === 0) return [];
        return lenses.map((lens) => {
          const range = new vscode.Range(
            lens.range.start.line,
            lens.range.start.character,
            lens.range.end.line,
            lens.range.end.character,
          );
          const codeLens = new vscode.CodeLens(range);
          if (lens.command) {
            codeLens.command = {
              title: lens.command.title,
              command: lens.command.command,
              arguments: lens.command.arguments ?? [],
            };
          }
          return codeLens;
        });
      } catch {
        return [];
      }
    },
  };

  context.subscriptions.push(
    vscode.languages.registerCodeLensProvider({ language: 'csharp' }, provider),
  );
}
