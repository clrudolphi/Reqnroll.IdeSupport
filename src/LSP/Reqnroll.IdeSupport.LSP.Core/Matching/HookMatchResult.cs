using Reqnroll.IdeSupport.LSP.Core.Bindings;

namespace Reqnroll.IdeSupport.LSP.Core.Matching;
public class HookMatchResult
{
    public ProjectHookBinding[] Items { get; }

    public bool HasHooks => Items.Length > 0;

    public HookMatchResult(ProjectHookBinding[] items)
    {
        Items = items;
    }
}
