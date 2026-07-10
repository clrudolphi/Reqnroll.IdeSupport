#nullable enable

using System.Collections.Generic;

namespace Reqnroll.IdeSupport.VisualStudio.Extension.RenameStep;

/// <summary>
/// Client-side result for the <c>reqnroll/renameTargets</c> response (Step Rename refactoring).
/// Mirrors the fields of <see cref="LSP.Server.Protocol.RenameTargetsResponse"/>
/// without a compilation dependency on the server project.
/// </summary>
internal sealed class RenameTargetsResult
{
    /// <summary>The renameable binding attributes found at the queried position.</summary>
    public List<RenameTargetItem> Targets { get; } = new();
}

/// <summary>One renameable binding attribute at the queried position.</summary>
internal sealed class RenameTargetItem
{
    /// <summary>Display label shown in the picker (e.g. class/method and expression).</summary>
    public string Label { get; init; } = "";
    /// <summary>The binding's expression/regex text.</summary>
    public string Expression { get; init; } = "";
    /// <summary>Index of this attribute among the binding method's step-definition attributes.</summary>
    public int AttributeIndex { get; init; }
}
