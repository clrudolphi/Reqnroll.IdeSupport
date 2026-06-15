#nullable enable

using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Reqnroll.IdeSupport.LSP.Core.Rename;

/// <summary>
/// Builds feature-step replacement text that preserves original parameter
/// values when a binding expression is renamed.
/// <para>
/// Matches the old binding regex against the original step text to extract
/// captured parameter values, then injects them into parameter slots of the
/// new expression (regex capturing groups or cucumber expression parameters).
/// </para>
/// </summary>
public static class FeatureStepTextBuilder
{
    /// <summary>
    /// Builds the replacement text for a feature step by matching the old regex
    /// against the original step text, extracting captured parameter values, and
    /// injecting them into parameter slots of the new expression.
    /// <para>
    /// If the regex does not match or no captures are found, returns <paramref name="newName"/>
    /// as-is (fallback to full-range replacement).
    /// </para>
    /// </summary>
    public static string Build(
        string newName,
        Regex? regex,
        string? stepText)
    {
        if (regex == null || string.IsNullOrEmpty(stepText))
            return newName;

        var match = regex.Match(stepText);
        if (!match.Success || match.Groups.Count <= 1)
            return newName;

        // Extract captured group values (group 0 = full match, groups 1..N = params)
        var capturedValues = new List<string>();
        for (int i = 1; i < match.Groups.Count; i++)
            capturedValues.Add(match.Groups[i].Value);

        if (capturedValues.Count == 0)
            return newName;

        // Replace parameter slots in the new expression with captured values.
        // Parameter slots are regex capturing groups (...) or cucumber expression
        // parameters {...}. We scan the new expression for these and replace them
        // in order with the captured values.
        var result = new StringBuilder();
        int groupIdx = 0;
        int lastEnd = 0;

        for (int i = 0; i < newName.Length; i++)
        {
            // Detect start of a capturing group: unescaped '(' not followed by '?:'
            if (newName[i] == '(' && (i == 0 || newName[i - 1] != '\\'))
            {
                // Skip non-capturing groups (?:, (?=, (?!, (?<=, (?<!) and named groups
                if (i + 1 < newName.Length && newName[i + 1] == '?' && i + 2 < newName.Length)
                {
                    var lookahead = newName.Substring(i + 2, 1);
                    if (lookahead is ":" or "=" or "!" or "<")
                        continue; // non-capturing group, skip
                }

                // Find matching ')' accounting for nesting
                int depth = 1;
                int j = i + 1;
                while (j < newName.Length && depth > 0)
                {
                    if (newName[j] == '(' && newName[j - 1] != '\\') depth++;
                    else if (newName[j] == ')' && newName[j - 1] != '\\') depth--;
                    j++;
                }

                // Append static text before this group
                result.Append(newName, lastEnd, i - lastEnd);

                // Replace with captured value
                if (groupIdx < capturedValues.Count)
                    result.Append(capturedValues[groupIdx]);
                groupIdx++;

                lastEnd = j; // skip past ')'
                i = j - 1;   // loop increment will move past ')'
            }
            // Detect cucumber expression parameter {...}
            else if (newName[i] == '{')
            {
                int j = i + 1;
                while (j < newName.Length && newName[j] != '}') j++;
                if (j < newName.Length)
                {
                    result.Append(newName, lastEnd, i - lastEnd);
                    if (groupIdx < capturedValues.Count)
                        result.Append(capturedValues[groupIdx]);
                    groupIdx++;
                    lastEnd = j + 1;
                    i = j; // loop increment moves past '}'
                }
            }
        }

        // Append remaining static text
        if (lastEnd < newName.Length)
            result.Append(newName, lastEnd, newName.Length - lastEnd);

        return result.Length > 0 ? result.ToString() : newName;
    }
}
