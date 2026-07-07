using System.Linq;
using Newtonsoft.Json.Linq;

namespace Reqnroll.IdeSupport.VisualStudio.Extension.LspInterception;

/// <summary>
/// Normalizes the drive-letter casing of <c>file:///</c> URIs found anywhere in a parsed
/// JSON-RPC message body, in place.
/// </summary>
/// <remarks>
/// OmniSharp.Extensions.LanguageServer 0.19.9's <c>DocumentUri</c> type unconditionally
/// lowercases the drive letter of any file path it serializes, regardless of the construction
/// path used server-side. Visual Studio, however, opens and tracks documents using the original
/// casing supplied by the project system — almost always an upper-case drive letter. Any
/// server-emitted URI that VS compares against its own document identity (Go To Definition,
/// Rename Step, Go To Hooks) therefore fails to match by case alone, even though the path is
/// otherwise correct (see issue #65).
///
/// Applying <see cref="ToUpperInvariant"/> to the drive letter is a safe, idempotent fix: VS
/// itself always uses an upper-case drive letter, so rewriting every server→VS URI to match
/// cannot break a client that was already using that casing, and it is harmless for URIs that
/// don't have a drive-letter segment at all (UNC paths, non-file schemes).
/// </remarks>
internal static class DriveLetterUriNormalizer
{
    private const string FileUriPrefix = "file:///";

    /// <summary>
    /// Walks <paramref name="token"/> (objects, arrays, and scalars) and upper-cases the drive
    /// letter of any <c>file:///&lt;letter&gt;:/...</c> string found — whether it appears as a
    /// JSON value or as an object property name (e.g. the URI keys of a <c>WorkspaceEdit.changes</c>
    /// map). Returns <see langword="true"/> if anything was changed.
    /// </summary>
    public static bool NormalizeInPlace(JToken token)
    {
        var changed = false;

        switch (token)
        {
            case JObject obj:
                // Snapshot properties before renaming any of them, since renaming mutates
                // the object's property collection.
                foreach (var prop in obj.Properties().ToList())
                {
                    if (TryUppercaseDriveLetter(prop.Name, out var newName))
                    {
                        var value = prop.Value;
                        obj.Remove(prop.Name);
                        obj.Add(newName, value);
                        changed = true;
                    }
                }

                foreach (var prop in obj.Properties())
                {
                    if (NormalizeInPlace(prop.Value))
                        changed = true;
                }
                break;

            case JArray arr:
                foreach (var item in arr)
                {
                    if (NormalizeInPlace(item))
                        changed = true;
                }
                break;

            case JValue { Type: JTokenType.String } val:
                var str = val.Value<string>();
                if (str is not null && TryUppercaseDriveLetter(str, out var newStr))
                {
                    val.Value = newStr;
                    changed = true;
                }
                break;
        }

        return changed;
    }

    /// <summary>
    /// If <paramref name="value"/> starts with <c>file:///&lt;lowercase-letter&gt;:</c>,
    /// returns <see langword="true"/> and <paramref name="result"/> with that letter
    /// upper-cased. Otherwise returns <see langword="false"/> and echoes back the input.
    /// </summary>
    private static bool TryUppercaseDriveLetter(string value, out string result)
    {
        result = value;

        var driveLetterIndex = FileUriPrefix.Length;
        if (value.Length <= driveLetterIndex + 1 ||
            !value.StartsWith(FileUriPrefix, System.StringComparison.Ordinal))
            return false;

        var driveLetter = value[driveLetterIndex];
        if (!char.IsLower(driveLetter) || value[driveLetterIndex + 1] != ':')
            return false;

        var chars = value.ToCharArray();
        chars[driveLetterIndex] = char.ToUpperInvariant(driveLetter);
        result = new string(chars);
        return true;
    }
}
