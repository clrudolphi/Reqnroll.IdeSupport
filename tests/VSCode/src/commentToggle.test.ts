import * as assert from 'node:assert';

// Import from the compiled extension output.  selectionUtils.ts itself has no vscode
// imports, but importing directly from the .ts source would drag in commentToggle.ts
// which does, breaking plain-Node.js Mocha.  Requires the extension to be compiled:
//   cd src/VSCode && npm run compile
// eslint-disable-next-line @typescript-eslint/no-require-imports
const { normalizeSelectionLines } = require('../../../src/VSCode/out/selectionUtils') as {
  normalizeSelectionLines(startLine: number, endLine: number, endChar: number): [number, number];
};

describe('normalizeSelectionLines', () => {
  it('single-line selection is unchanged', () => {
    const [start, end] = normalizeSelectionLines(3, 3, 0);
    assert.strictEqual(start, 3);
    assert.strictEqual(end, 3);
  });

  it('multi-line selection with non-zero end character is unchanged', () => {
    const [start, end] = normalizeSelectionLines(1, 4, 5);
    assert.strictEqual(start, 1);
    assert.strictEqual(end, 4);
  });

  it('multi-line selection ending at col 0 reduces endLine by one', () => {
    // Cursor landed at the start of line 4 without selecting any content on it
    const [start, end] = normalizeSelectionLines(1, 4, 0);
    assert.strictEqual(start, 1);
    assert.strictEqual(end, 3);
  });

  it('two-line selection ending at col 0 collapses to single line', () => {
    const [start, end] = normalizeSelectionLines(2, 3, 0);
    assert.strictEqual(start, 2);
    assert.strictEqual(end, 2);
  });
});
