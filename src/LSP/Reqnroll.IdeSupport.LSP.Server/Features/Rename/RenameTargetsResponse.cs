#nullable enable

using System.Collections.Generic;
using Newtonsoft.Json;

namespace Reqnroll.IdeSupport.LSP.Server.Features.Rename;

/// <summary>Response DTO for the custom reqnroll/renameTargets request (Step Rename refactoring).</summary>
public sealed class RenameTargetsResponse
{
    /// <summary>Gets or sets the targets.</summary>
    [JsonProperty("targets")]
    public List<RenameTargetItem> Targets { get; set; } = new();
}

/// <summary>One renameable binding attribute at the queried position.</summary>
public sealed class RenameTargetItem
{
    /// <summary>Gets or sets the label.</summary>
    [JsonProperty("label")]
    public string Label { get; set; } = "";

    /// <summary>
    /// The bare step expression as written in the source attribute (e.g. <c>the first number is {int}</c>),
    /// without the step-type prefix or scope suffix. This is what the rename dialog should seed so the
    /// user edits the live expression form (preserving Cucumber parameter types) rather than a regex
    /// projection. Falls back to the registry expression when the source literal cannot be resolved.
    /// </summary>
    [JsonProperty("expression")]
    public string Expression { get; set; } = "";

    /// <summary>Gets or sets the attribute index.</summary>
    [JsonProperty("attributeIndex")]
    public int AttributeIndex { get; set; }

    /// <summary>Gets or sets the start line.</summary>
    [JsonProperty("startLine")]
    public int StartLine { get; set; }

    /// <summary>Gets or sets the start char.</summary>
    [JsonProperty("startChar")]
    public int StartChar { get; set; }

    /// <summary>Gets or sets the end line.</summary>
    [JsonProperty("endLine")]
    public int EndLine { get; set; }

    /// <summary>Gets or sets the end char.</summary>
    [JsonProperty("endChar")]
    public int EndChar { get; set; }
}
