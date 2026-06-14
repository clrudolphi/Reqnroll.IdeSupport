Feature: Code Folding (F10)

textDocument/foldingRange returns foldable region ranges for Gherkin document elements
so the IDE can render collapse markers in the editor gutter.

Background:
    Given the LSP server is started

# ── Feature-only file ────────────────────────────────────────────────────────

Scenario: Feature with no body produces no folding ranges
    When the feature file "Empty.feature" is opened with
        """
        Feature: Calculator
        """
    And the code folding ranges are requested for "Empty.feature"
    Then the folding ranges are empty

Scenario: Feature with one scenario produces feature body and scenario folds
    When the feature file "OneScenario.feature" is opened with
        """
        Feature: F
        Scenario: Add
            Given a step
        """
    And the code folding ranges are requested for "OneScenario.feature"
    Then the folding range count is 2
    And a folding range exists from line 1 to line 2

# ── Multiple scenarios ────────────────────────────────────────────────────────

Scenario: Multiple scenarios each produce their own fold
    When the feature file "MultiScenario.feature" is opened with
        """
        Feature: F
        Scenario: S1
            Given step1
        Scenario: S2
            Given step2
        """
    And the code folding ranges are requested for "MultiScenario.feature"
    Then the folding range count is 3
    And a folding range exists from line 1 to line 2
    And a folding range exists from line 3 to line 4

# ── Background ────────────────────────────────────────────────────────────────

Scenario: Background produces a folding range
    When the feature file "WithBackground.feature" is opened with
        """
        Feature: F
        Background:
            Given a setup step
        Scenario: S
            Given a step
        """
    And the code folding ranges are requested for "WithBackground.feature"
    Then a folding range exists from line 1 to line 2

# ── Rule ─────────────────────────────────────────────────────────────────────

Scenario: Rule produces a folding range
    When the feature file "WithRule.feature" is opened with
        """
        Feature: F
        Rule: Business rule
        Scenario: S
            Given a step
        """
    And the code folding ranges are requested for "WithRule.feature"
    Then a folding range exists from line 1 to line 3

# ── Scenario Outline + Examples ───────────────────────────────────────────────

Scenario: Scenario Outline and Examples both produce folds
    When the feature file "WithOutline.feature" is opened with
        """
        Feature: F
        Scenario Outline: SO
            Given the number is <n>
            Examples:
                | n |
                | 1 |
                | 2 |
        """
    And the code folding ranges are requested for "WithOutline.feature"
    Then a folding range exists from line 1 to line 6
    And a folding range exists from line 3 to line 6

# ── Data Table ────────────────────────────────────────────────────────────────

Scenario: Data table produces a folding range
    When the feature file "WithTable.feature" is opened with
        """
        Feature: F
        Scenario: Tabular
            Given I have:
                | a | b |
                | 1 | 2 |
            Then done
        """
    And the code folding ranges are requested for "WithTable.feature"
    Then a folding range exists from line 3 to line 4

# ── Doc String (using backtick delimiters to avoid Reqnroll doc-string collision) ─

Scenario: Doc string produces a folding range
    When the feature file "WithDocString.feature" is opened with
        """
        Feature: F
        Scenario: Doc
            Given a step
            ```
            some doc
            string
            ```
            Then done
        """
    And the code folding ranges are requested for "WithDocString.feature"
    Then a folding range exists from line 3 to line 6

# ── Non-.feature file ─────────────────────────────────────────────────────────

Scenario: Folding range request on a non-feature file returns no ranges
    Given the file "Notes.txt" is open with content
        """
        some text
        """
    When the code folding ranges are requested for "Notes.txt"
    Then the folding ranges are empty
