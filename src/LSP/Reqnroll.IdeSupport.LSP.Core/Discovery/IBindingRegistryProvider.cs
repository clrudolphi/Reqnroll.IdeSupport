
namespace Reqnroll.IdeSupport.LSP.Core.Discovery;

/// <summary>
/// Provides the current ProjectBindingRegistry and notifies when it is replaced.
/// VS implementation: thin wrapper over IDiscoveryService.BindingRegistryCache.
/// LSP implementation: populated by MSBuild/JSON connector-based discovery.
/// </summary>
public interface IBindingRegistryProvider
{
    ProjectBindingRegistry Current { get; }

    /// <summary>Raised on any thread when the registry is replaced.</summary>
    event EventHandler BindingRegistryChanged;
}