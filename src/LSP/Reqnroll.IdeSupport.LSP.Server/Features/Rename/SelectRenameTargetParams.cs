#nullable enable

using Newtonsoft.Json;
using OmniSharp.Extensions.LanguageServer.Protocol;

namespace Reqnroll.IdeSupport.LSP.Server.Features.Rename;

/// <summary>Parameters for reqnroll/selectRenameTarget notification (F16).</summary>
public sealed class SelectRenameTargetParams
{
    // DocumentUri (not string): every other URI-keyed lookup in this codebase (MatchSetKey,
    // DocumentBufferService) sources both the write-key and read-key from a DocumentUri.ToString()
    // round-trip, which is why they never see client URI-encoding quirks (e.g. VS Code's
    // Uri.toString() percent-encoding the Windows drive-letter colon). A raw string field here
    // was the actual root cause of a bug where RenameSessionManager's key from this notification
    // never matched HandleRenameAsync's DocumentUri-derived key for real client traffic — typing
    // this the same way the rest of the codebase does removes the mismatch at its source, rather
    // than relying solely on string-normalization to paper over it.
    [JsonProperty("uri")]
    public DocumentUri Uri { get; set; } = null!;

    [JsonProperty("version")]
    public int Version { get; set; }

    [JsonProperty("attributeIndex")]
    public int AttributeIndex { get; set; }
}
