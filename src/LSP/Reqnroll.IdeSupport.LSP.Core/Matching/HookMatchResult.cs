using Reqnroll.IdeSupport.LSP.Core.Bindings;

namespace Reqnroll.IdeSupport.LSP.Core.Matching;

/// <summary>The set of hook bindings that apply to a given scope (e.g. a feature or scenario).</summary>
public class HookMatchResult
{
    /// <summary>The matched hook bindings.</summary>
    public ProjectHookBinding[] Items { get; }

    /// <summary>True when at least one hook binding matched.</summary>
    public bool HasHooks => Items.Length > 0;

    /// <summary>Creates a hook match result from the matched hook bindings.</summary>
    public HookMatchResult(ProjectHookBinding[] items)
    {
        Items = items;
    }
}
