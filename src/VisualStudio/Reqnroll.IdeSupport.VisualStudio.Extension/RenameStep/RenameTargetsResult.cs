#nullable enable

using System.Collections.Generic;

namespace Reqnroll.IdeSupport.VisualStudio.Extension.RenameStep;

/// <summary>
/// Client-side result for the <c>reqnroll/renameTargets</c> response (F16).
/// Mirrors the fields of <see cref="LSP.Server.Protocol.RenameTargetsResponse"/>
/// without a compilation dependency on the server project.
/// </summary>
internal sealed class RenameTargetsResult
{
    public List<RenameTargetItem> Targets { get; } = new();

    /// <summary>
    /// True when <see cref="Targets"/> is empty because the cursor is on a step that matches
    /// more than one binding, none of which resolve to a single definite match — as opposed to
    /// no binding being found at all.
    /// </summary>
    public bool IsAmbiguous { get; init; }
}

/// <summary>One renameable binding attribute at the queried position.</summary>
internal sealed class RenameTargetItem
{
    public string Label { get; init; } = "";
    public string Expression { get; init; } = "";
    public int AttributeIndex { get; init; }
}
