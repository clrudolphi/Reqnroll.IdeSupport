import * as fs from 'fs';
import * as os from 'os';
import * as path from 'path';
import * as vscode from 'vscode';

/**
 * A VS Code LogOutputChannel that simultaneously writes LSP trace messages to
 * the VS Code Output panel and to a timestamped log file in lsp-viewer format.
 *
 * Why LogOutputChannel (not plain OutputChannel):
 *   vscode-languageclient v10 types traceOutputChannel as LogOutputChannel and
 *   reads its logLevel property at construction time to set the internal trace
 *   level.  We always return LogLevel.Trace from logLevel so the client enables
 *   tracing and routes messages to channel.trace().
 *
 * Why only trace() writes to file:
 *   vscode-languageclient routes all LSP request/response/notification entries
 *   through channel.trace().  debug()/info()/warn()/error() carry general client
 *   diagnostics (connection state, errors) but not the per-message trace lines.
 *   The lsp-viewer only needs the per-message entries, so those are the only ones
 *   tee-d to the file.
 *
 * File format:
 *   Each entry is written as:
 *     [LSP - HH:MM:SS AM] <message text>\n\n
 *   vscode-jsonrpc formats the message text as human-readable lines, e.g.:
 *     Sending request 'textDocument/hover - (1)'.
 *     Params: { ... }
 *   The [LSP - HH:MM:SS AM] prefix is the delimiter the lampepfl lsp-viewer uses
 *   to identify and separate entries (https://lampepfl.github.io/lsp-viewer/).
 *
 * File path convention:
 *   Windows : %LOCALAPPDATA%\Reqnroll\reqnroll-vscode-inspector-<ts>.log
 *   macOS   : ~/Library/Logs/Reqnroll/reqnroll-vscode-inspector-<ts>.log
 *   Linux   : ~/.local/share/Reqnroll/reqnroll-vscode-inspector-<ts>.log
 */
class TeeLogOutputChannel implements vscode.LogOutputChannel {
  readonly name: string;
  private readonly _inner: vscode.LogOutputChannel;
  private _stream: fs.WriteStream | undefined;

  constructor(name: string, stream: fs.WriteStream | undefined) {
    this._inner = vscode.window.createOutputChannel(name, { log: true });
    this.name = this._inner.name;
    this._stream = stream;
  }

  get logLevel(): vscode.LogLevel {
    // Always report Trace so vscode-languageclient enables tracing and routes
    // all LSP messages to channel.trace() rather than suppressing them.
    return vscode.LogLevel.Trace;
  }

  get onDidChangeLogLevel(): vscode.Event<vscode.LogLevel> {
    return this._inner.onDidChangeLogLevel;
  }

  trace(message: string, ...args: unknown[]): void {
    this._inner.trace(message, ...args);
    this._writeLspEntry(message);
  }

  // General client diagnostics — forward to panel only, not to the trace file.
  debug(message: string, ...args: unknown[]): void { this._inner.debug(message, ...args); }
  info(message: string, ...args: unknown[]): void { this._inner.info(message, ...args); }
  warn(message: string, ...args: unknown[]): void { this._inner.warn(message, ...args); }
  error(message: string | Error, ...args: unknown[]): void { this._inner.error(message, ...args); }

  append(value: string): void { this._inner.append(value); }
  appendLine(value: string): void { this._inner.appendLine(value); }
  replace(value: string): void { this._inner.replace(value); }
  clear(): void { this._inner.clear(); }

  show(preserveFocus?: boolean): void;
  show(column?: vscode.ViewColumn, preserveFocus?: boolean): void;
  show(_colOrFocus?: vscode.ViewColumn | boolean, _focus?: boolean): void {
    this._inner.show();
  }

  hide(): void { this._inner.hide(); }

  dispose(): void {
    this._inner.dispose();
    this._stream?.end();
    this._stream = undefined;
  }

  private _writeLspEntry(message: string): void {
    if (!this._stream) return;
    const time = new Date().toLocaleTimeString('en-US', {
      hour: '2-digit',
      minute: '2-digit',
      second: '2-digit',
    });
    // Blank line after each entry acts as the separator the lsp-viewer expects
    // between consecutive [LSP - ...] blocks.
    this._stream.write(`[LSP - ${time}] ${message}\n\n`);
  }
}

/**
 * Creates a trace output channel whose verbosity is controlled by the
 * `reqnroll.trace.server` VS Code setting (`off` | `messages` | `verbose`).
 *
 * When the setting is not `off`, a log file is opened in the Reqnroll log
 * directory so that every JSON-RPC message captured by vscode-languageclient
 * is persisted in lsp-viewer format alongside the VS Code Output panel entry.
 */
export function createTraceChannel(): vscode.LogOutputChannel {
  const level = vscode.workspace.getConfiguration('reqnroll').get<string>('trace.server', 'off');

  if (level === 'off') {
    return vscode.window.createOutputChannel('Reqnroll LSP Trace', { log: true });
  }

  let stream: fs.WriteStream | undefined;
  try {
    const logDir = resolveLogDirectory();
    fs.mkdirSync(logDir, { recursive: true });
    const ts = new Date().toISOString().replace(/[:.]/g, '-').slice(0, 19);
    const logPath = path.join(logDir, `reqnroll-vscode-inspector-${ts}.log`);
    stream = fs.createWriteStream(logPath, { flags: 'a' });
  } catch {
    // File logging unavailable; the VS Code output channel is the fallback.
  }

  return new TeeLogOutputChannel('Reqnroll LSP Trace', stream);
}

function resolveLogDirectory(): string {
  switch (process.platform) {
    case 'win32':
      return path.join(process.env['LOCALAPPDATA'] ?? os.homedir(), 'Reqnroll');
    case 'darwin':
      return path.join(os.homedir(), 'Library', 'Logs', 'Reqnroll');
    default:
      return path.join(os.homedir(), '.local', 'share', 'Reqnroll');
  }
}
