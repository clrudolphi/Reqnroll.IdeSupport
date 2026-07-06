import * as assert from 'assert';
import { getPipeIndexes, getTrimmedCellRange, isTableRow } from '../tableHighlightService';

suite('tableHighlightService', () => {
  suite('isTableRow', () => {
    test('recognizes a well-formed table row', () => {
      assert.strictEqual(isTableRow('\t\t| operand | type |'), true);
    });

    test('rejects a line with no pipes', () => {
      assert.strictEqual(isTableRow('\t\tGiven the operands entered'), false);
    });

    test('rejects a line with only one pipe', () => {
      assert.strictEqual(isTableRow('\t\t| operand'), false);
    });

    test('rejects a line where the first non-whitespace character is not a pipe', () => {
      assert.strictEqual(isTableRow('\t\tfoo | bar |'), false);
    });
  });

  suite('getPipeIndexes', () => {
    test('returns the index of every pipe character', () => {
      assert.deepStrictEqual(getPipeIndexes('| a | b |'), [0, 4, 8]);
    });

    test('returns an empty array when there are no pipes', () => {
      assert.deepStrictEqual(getPipeIndexes('no pipes here'), []);
    });
  });

  suite('getTrimmedCellRange', () => {
    test('trims leading and trailing whitespace from the cell content', () => {
      const range = getTrimmedCellRange(0, '|  operand  |', 0, 12);

      assert.ok(range);
      assert.strictEqual(range.start.character, 3);
      assert.strictEqual(range.end.character, 10);
    });

    test('returns undefined for an empty cell', () => {
      const range = getTrimmedCellRange(0, '|    |', 0, 5);

      assert.strictEqual(range, undefined);
    });

    test('returns undefined for a whitespace-only cell', () => {
      const range = getTrimmedCellRange(0, '|   |', 0, 4);

      assert.strictEqual(range, undefined);
    });
  });
});
