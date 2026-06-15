#nullable enable

using System.Collections.Generic;
using Newtonsoft.Json;

namespace Reqnroll.IdeSupport.LSP.Server.Protocol;

/// <summary>Response DTO for the custom reqnroll/renameTargets request (F16).</summary>
public sealed class RenameTargetsResponse
{
    [JsonProperty("targets")]
    public List<RenameTargetItem> Targets { get; set; } = new();
}

/// <summary>One renameable binding attribute at the queried position.</summary>
public sealed class RenameTargetItem
{
    [JsonProperty("label")]
    public string Label { get; set; } = "";

    [JsonProperty("attributeIndex")]
    public int AttributeIndex { get; set; }

    [JsonProperty("startLine")]
    public int StartLine { get; set; }

    [JsonProperty("startChar")]
    public int StartChar { get; set; }

    [JsonProperty("endLine")]
    public int EndLine { get; set; }

    [JsonProperty("endChar")]
    public int EndChar { get; set; }
}
