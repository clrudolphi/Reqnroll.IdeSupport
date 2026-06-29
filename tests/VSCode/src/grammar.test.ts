import * as assert from 'node:assert';
import * as fs from 'node:fs';
import * as path from 'node:path';

/**
 * Structural tests for the TextMate grammar (gherkin.tmLanguage.json).
 *
 * Validates the grammar JSON is well-formed and each repository entry
 * has the expected structure and regex patterns compile and match
 * the intended Gherkin constructs.
 */

let grammar: Record<string, any>;

before(() => {
  grammar = JSON.parse(
    fs.readFileSync(
      path.resolve(__dirname, '..', '..', '..', 'src', 'VSCode', 'syntaxes', 'gherkin.tmLanguage.json'),
      'utf-8',
    ),
  );
});

describe('gherkin.tmLanguage.json', () => {
  it('should have a top-level patterns array with include references', () => {
    assert.ok(Array.isArray(grammar.patterns));
    assert.ok(grammar.patterns.length >= 8);
    for (const ref of grammar.patterns) {
      assert.ok(typeof ref.include === 'string', `Expected include reference`);
    }
  });

  it('should have all required repository keys', () => {
    const expected = [
      'comments', 'doc_strings', 'tags', 'feature_keywords',
      'step_keywords', 'table_header_separator', 'tables',
      'strings', 'scenario_outline_placeholders', 'numeric_literals',
    ];
    for (const key of expected) {
      assert.ok(key in grammar.repository, `Missing repository key: ${key}`);
    }
  });

  // ── Comments ────────────────────────────────────────────────────────────

  describe('comments', () => {
    const p = () => grammar.repository.comments.patterns[0];

    it('should have comment.line.gherkin scope', () => {
      assert.strictEqual(p().name, 'comment.line.gherkin');
    });

    it('should match # comments', () => {
      const re = new RegExp(p().match);
      assert.ok(re.test('# this is a comment'));
      assert.ok(re.test('  # indented comment'));
      assert.ok(!re.test('Given something'));
    });
  });

  // ── Tags ────────────────────────────────────────────────────────────────

  describe('tags', () => {
    const p = () => grammar.repository.tags.patterns[0];

    it('should match individual @tags anywhere on a line', () => {
      const re = new RegExp(p().match!, 'g');
      assert.deepStrictEqual('@smoke'.match(re), ['@smoke']);
      const matches = '@smoke @regression @slow'.match(re);
      assert.strictEqual(matches?.length, 3);
    });
  });

  // ── Feature keywords ────────────────────────────────────────────────────

  describe('feature_keywords', () => {
    const p = () => grammar.repository.feature_keywords.patterns[0];

    it('should match each Gherkin block keyword', () => {
      const re = new RegExp(p().match);
      assert.ok(re.test('Feature: Login'));
      assert.ok(re.test('Rule: Access control'));
      assert.ok(re.test('Background:'));
      assert.ok(re.test('Scenario: Successful login'));
      assert.ok(re.test('Scenario Outline: Login variants'));
      assert.ok(re.test('Scenario Template: Login variants'));
      assert.ok(re.test('Examples:'));
      assert.ok(re.test('Example:'));
    });

    it('should match indented keywords', () => {
      const re = new RegExp(p().match);
      assert.ok(re.test('  Scenario: indented'));
    });

    it('should not match step keywords', () => {
      const re = new RegExp(p().match);
      assert.ok(!re.test('Given something'));
    });
  });

  // ── Step keywords ───────────────────────────────────────────────────────

  describe('step_keywords', () => {
    const p0 = () => grammar.repository.step_keywords.patterns[0];
    const p1 = () => grammar.repository.step_keywords.patterns[1];

    it('should match each Given/When/Then/And/But keyword', () => {
      const re = new RegExp(p0().match);
      assert.ok(re.test('Given I have 42'));
      assert.ok(re.test('When I press enter'));
      assert.ok(re.test('Then I see result'));
      assert.ok(re.test('And something else'));
      assert.ok(re.test('But not this'));
    });

    it('should match * step keyword as a separate pattern', () => {
      const re = new RegExp(p1().match);
      assert.ok(re.test('* some step'));
      assert.ok(re.test('  * indented asterisk step'));
    });

    it('should match indented step keywords', () => {
      const re = new RegExp(p0().match);
      assert.ok(re.test('    Given indented step'));
    });

    it('should have keyword.control.gherkin.step scope', () => {
      assert.strictEqual(p0().name, 'keyword.control.gherkin.step');
    });
  });

  // ── Doc strings ─────────────────────────────────────────────────────────

  describe('doc_strings', () => {
    const p = () => grammar.repository.doc_strings;

    it('should use begin/end for triple-quoted blocks', () => {
      assert.strictEqual(p().begin, '"""');
      assert.strictEqual(p().end, '"""');
      assert.strictEqual(p().name, 'string.quoted.other.gherkin');
    });
  });

  // ── Table header separator ──────────────────────────────────────────────

  describe('table_header_separator', () => {
    const p = () => grammar.repository.table_header_separator.patterns[0];

    it('should match separator rows with only dashes/colons/pipes', () => {
      const re = new RegExp(p().match);
      assert.ok(re.test('|------|--------|'));
      assert.ok(re.test('  |---|----|'));
      // Should NOT match data rows with text
      assert.ok(!re.test('| name | value |'));
    });
  });

  // ── Tables ──────────────────────────────────────────────────────────────

  describe('tables', () => {
    const p = () => grammar.repository.tables;

    it('should use begin/while for multi-row blocks', () => {
      assert.ok(typeof p().patterns[0].begin === 'string');
      assert.ok(typeof p().patterns[0].while === 'string');
    });

    it('should have begin pattern matching pipe start', () => {
      const re = new RegExp(p().patterns[0].begin);
      assert.ok(re.test('| name | value |'));
    });

    it('should have while pattern continuing on pipe lines', () => {
      const re = new RegExp(p().patterns[0].while);
      assert.ok(re.test('| value1 | value2 |'));
    });
  });

  // ── Strings ─────────────────────────────────────────────────────────────

  describe('strings', () => {
    const p = () => grammar.repository.strings.patterns[0];

    it('should match double-quoted strings', () => {
      const re = new RegExp(p().match);
      assert.ok(re.test('"hello"'));
      assert.ok(re.test('"with space"'));
    });
  });

  // ── Scenario outline placeholders ───────────────────────────────────────

  describe('scenario_outline_placeholders', () => {
    const p = () => grammar.repository.scenario_outline_placeholders.patterns[0];

    it('should match <placeholder>', () => {
      const re = new RegExp(p().match);
      assert.ok(re.test('<name>'));
      assert.ok(re.test('<some-value>'));
    });
  });

  // ── Numeric literals ────────────────────────────────────────────────────

  describe('numeric_literals', () => {
    const p = () => grammar.repository.numeric_literals.patterns[0];

    it('should match integers and decimals', () => {
      const re = new RegExp(p().match);
      assert.ok(re.test('42'));
      assert.ok(re.test('3.14'));
    });

    it('should not match non-numeric words', () => {
      const re = new RegExp(p().match);
      assert.ok(!re.test('hello'));
    });
  });
});
