using System;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Reqnroll.IdeSupport.LSP.Connector.Models;

/// <summary>
/// STJ-based serialization helpers for the out-of-process connector wire protocol.
/// The connector writes a marker-delimited JSON block to stdout:
/// <code>
/// &gt;&gt;&gt;&gt;&gt;&gt;&gt;&gt;&gt;&gt;
/// { ... }
/// &lt;&lt;&lt;&lt;&lt;&lt;&lt;&lt;&lt;&lt;
/// </code>
/// This class is the single source of truth for the markers; the connector exe uses the same
/// constants when writing, and the server-side launcher uses this class when reading.
/// </summary>
public static class ConnectorJsonSerialization
{
    public const string StartMarker = ">>>>>>>>>>";
    public const string EndMarker   = "<<<<<<<<<<";

    // ── Deserialize (used by LSP.Server launcher) ──────────────────────────

    /// <summary>
    /// Extracts the JSON payload delimited by <see cref="StartMarker"/> / <see cref="EndMarker"/>
    /// from <paramref name="rawOutput"/> and deserializes it as <typeparamref name="T"/>.
    /// Returns <see langword="default"/> when the markers cannot be found or parsing fails.
    /// </summary>
    public static T? DeserializeObjectWithMarker<T>(string rawOutput)
    {
        if (string.IsNullOrEmpty(rawOutput))
            return default;

        var start = rawOutput.IndexOf(StartMarker, StringComparison.Ordinal);
        var end   = rawOutput.IndexOf(EndMarker,   StringComparison.Ordinal);

        if (start < 0 || end < 0 || end <= start)
            return default;

        var json = rawOutput
            .Substring(start + StartMarker.Length, end - start - StartMarker.Length)
            .Trim();

        return JsonSerializer.Deserialize<T>(json, ReadOptions);
    }

    // ── Serialize (used by the connector exe) ──────────────────────────────

    /// <summary>
    /// Serializes <paramref name="obj"/> as camelCase JSON, wrapped in start/end markers.
    /// </summary>
    public static string SerializeAndMark(object obj)
        => StartMarker + Environment.NewLine
                       + JsonSerializer.Serialize(obj, WriteOptions)
                       + Environment.NewLine
                       + EndMarker;

    // ── Options ────────────────────────────────────────────────────────────

    public static readonly JsonSerializerOptions WriteOptions = new()
    {
        PropertyNamingPolicy          = JsonNamingPolicy.CamelCase,
        WriteIndented                 = true,
        DefaultIgnoreCondition        = JsonIgnoreCondition.WhenWritingDefault,
        Encoder                       = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public static readonly JsonSerializerOptions ReadOptions = new()
    {
        PropertyNameCaseInsensitive   = true,
        DefaultIgnoreCondition        = JsonIgnoreCondition.WhenWritingDefault,
        Encoder                       = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };
}
