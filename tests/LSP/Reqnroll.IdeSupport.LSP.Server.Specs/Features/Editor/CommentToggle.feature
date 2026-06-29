Feature: Comment / Uncomment (F13)

workspace/executeCommand with reqnroll.toggleComment toggles # comments
on the selected line(s) of a .feature file, applying the edit via workspace/applyEdit.

Background:
    Given the LSP server is started

# ── Toggle ON (comment uncommented lines) ─────────────────────────────────────

Scenario: Toggle comment adds hash to a single uncommented line
    When the feature file "SingleLine.feature" is opened with
        """
        Feature: Calculator
        """
    And the toggle comment command is executed for "SingleLine.feature" on lines 0 to 0
    Then a workspace/applyEdit notification is sent
    And the edit replaces line 0 with "# Feature: Calculator"

Scenario: Toggle comment adds hash to multiple lines
    When the feature file "MultiLines.feature" is opened with
        """
        Feature: F
        Scenario: S
            Given a step
        """
    And the toggle comment command is executed for "MultiLines.feature" on lines 0 to 2
    Then the edit replaces line 0 with "# Feature: F"
    And the edit replaces line 1 with "# Scenario: S"
    And the edit replaces line 2 with "#     Given a step"

# ── Toggle OFF (uncomment all-commented lines) ────────────────────────────────

Scenario: Toggle comment removes hash from a single commented line
    When the feature file "Commented.feature" is opened with
        """
        # Feature: Calculator
        """
    And the toggle comment command is executed for "Commented.feature" on lines 0 to 0
    Then the edit replaces line 0 with "Feature: Calculator"

Scenario: Toggle comment removes hash from multiple commented lines
    When the feature file "MultiCommented.feature" is opened with
        """
        # Feature: F
        # Scenario: S
        #     Given a step
        """
    And the toggle comment command is executed for "MultiCommented.feature" on lines 0 to 2
    Then the edit replaces line 0 with "Feature: F"
    And the edit replaces line 1 with "Scenario: S"
    And the edit replaces line 2 with "    Given a step"

# ── Partial range ─────────────────────────────────────────────────────────────

Scenario: Only lines in the specified range are toggled
    When the feature file "PartialRange.feature" is opened with
        """
        Feature: F
        Scenario: S
            Given a step
        """
    And the toggle comment command is executed for "PartialRange.feature" on lines 1 to 1
    Then the edit replaces line 1 with "# Scenario: S"

# ── Mixed selection: not all commented → add hashes ──────────────────────────

Scenario: Toggle on a selection with mixed commented and uncommented lines adds hashes to all
    When the feature file "Mixed.feature" is opened with
        """
        # Feature: F
        Scenario: S
        """
    And the toggle comment command is executed for "Mixed.feature" on lines 0 to 1
    Then the edit replaces line 0 with "# # Feature: F"
    And the edit replaces line 1 with "# Scenario: S"

# ── Indented lines ────────────────────────────────────────────────────────────

Scenario: Toggle comment on indented step lines adds hash at column 0
    When the feature file "Indented.feature" is opened with
        """
        Feature: F
        Scenario: S
            Given a step
            When another step
        """
    And the toggle comment command is executed for "Indented.feature" on lines 2 to 3
    Then the edit replaces line 2 with "#     Given a step"
    And the edit replaces line 3 with "#     When another step"
