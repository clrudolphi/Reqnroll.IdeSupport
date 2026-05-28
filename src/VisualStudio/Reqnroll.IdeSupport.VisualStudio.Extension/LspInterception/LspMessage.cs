using Newtonsoft.Json.Linq;

namespace Reqnroll.IdeSupport.VisualStudio.Extension.LspInterception;

/// <summary>
/// A parsed JSON-RPC / LSP message travelling through the interception pipeline.
/// </summary>
internal sealed class LspMessage
{
    /// <param name="direction">Whether the message is being sent to or received from the server.</param>
    /// <param name="body">The full parsed JSON-RPC object.</param>
    /// <param name="timestamp">Wall-clock time when the message was observed.</param>
    public LspMessage(LspMessageDirection direction, JObject body, DateTimeOffset timestamp)
    {
        Direction = direction;
        Body      = body;
        Timestamp = timestamp;
        Method    = body["method"]?.Value<string>();
        Id        = body["id"];
    }

    /// <summary>Direction of travel.</summary>
    public LspMessageDirection Direction { get; }

    /// <summary>Full JSON-RPC message body.</summary>
    public JObject Body { get; }

    /// <summary>
    /// The <c>method</c> field, present on requests and notifications; <see langword="null"/> on responses.
    /// </summary>
    public string? Method { get; }

    /// <summary>
    /// The <c>id</c> field, present on requests and responses; <see langword="null"/> on notifications.
    /// </summary>
    public JToken? Id { get; }

    /// <summary>Wall-clock time when the message entered the pipeline.</summary>
    public DateTimeOffset Timestamp { get; }

    // ── Convenience classifiers ─────────────────────────────────────────────

    /// <summary>Has both <c>method</c> and <c>id</c> — a JSON-RPC request from client to server.</summary>
    public bool IsRequest => Method is not null && Id is not null;

    /// <summary>Has <c>id</c> but no <c>method</c> — a JSON-RPC response from server to client.</summary>
    public bool IsResponse => Method is null && Id is not null;

    /// <summary>Has <c>method</c> but no <c>id</c> — a JSON-RPC notification (no reply expected).</summary>
    public bool IsNotification => Method is not null && Id is null;
}
