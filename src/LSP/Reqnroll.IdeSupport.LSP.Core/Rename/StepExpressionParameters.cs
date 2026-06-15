#nullable enable

using System.Collections.Generic;
using System.Text;

namespace Reqnroll.IdeSupport.LSP.Core.Rename;

/// <summary>
/// Shared parsing of step-definition expression text into parameter slots and the static
/// (literal) segments around them. A parameter slot is a Cucumber placeholder (<c>{int}</c>,
/// <c>{string}</c>, <c>{}</c>, …) or a regex capturing group (an unescaped <c>(</c> that is not
/// a non-capturing / look-around group).
/// </summary>
public static class StepExpressionParameters
{
    /// <summary>
    /// Returns the length of the parameter slot starting at <paramref name="index"/> in
    /// <paramref name="s"/>, or 0 when no slot starts there.
    /// </summary>
    public static int SlotLengthAt(string s, int index)
    {
        var c = s[index];

        if (c == '{')
        {
            var j = index + 1;
            while (j < s.Length && s[j] != '}') j++;
            return j < s.Length ? j - index + 1 : 0;
        }

        if (c == '(' && (index == 0 || s[index - 1] != '\\'))
        {
            // Skip non-capturing / look-around groups: (?:  (?=  (?!  (?<
            if (index + 2 < s.Length && s[index + 1] == '?' &&
                (s[index + 2] == ':' || s[index + 2] == '=' || s[index + 2] == '!' || s[index + 2] == '<'))
                return 0;

            var depth = 1;
            var j = index + 1;
            while (j < s.Length && depth > 0)
            {
                if (s[j] == '(' && s[j - 1] != '\\') depth++;
                else if (s[j] == ')' && s[j - 1] != '\\') depth--;
                j++;
            }
            return depth == 0 ? j - index : 0;
        }

        return 0;
    }

    /// <summary>Returns the ordered parameter-slot substrings of <paramref name="expression"/>.</summary>
    public static List<string> ExtractSlots(string expression)
    {
        var slots = new List<string>();
        var i = 0;
        while (i < expression.Length)
        {
            var slotLength = SlotLengthAt(expression, i);
            if (slotLength > 0)
            {
                slots.Add(expression.Substring(i, slotLength));
                i += slotLength;
            }
            else
            {
                i++;
            }
        }
        return slots;
    }

    /// <summary>
    /// Splits <paramref name="expression"/> into its static (non-parameter) segments. An
    /// expression with N parameter slots yields N+1 segments (some possibly empty), in order.
    /// </summary>
    public static List<string> StaticSegments(string expression)
    {
        var segments = new List<string>();
        var sb = new StringBuilder();
        var i = 0;
        while (i < expression.Length)
        {
            var slotLength = SlotLengthAt(expression, i);
            if (slotLength > 0)
            {
                segments.Add(sb.ToString());
                sb.Clear();
                i += slotLength;
            }
            else
            {
                sb.Append(expression[i]);
                i++;
            }
        }
        segments.Add(sb.ToString());
        return segments;
    }
}
