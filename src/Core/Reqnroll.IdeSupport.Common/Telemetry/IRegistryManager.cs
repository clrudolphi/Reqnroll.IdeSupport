namespace Reqnroll.IdeSupport.Common.Telemetry;

/// <summary>IRegistryManager</summary>
public interface IRegistryManager
{
    /// <summary>Persists the given installation status to the registry. Returns whether the write succeeded.</summary>
    bool UpdateStatus(ReqnrollInstallationStatus status);
    /// <summary>Reads the current installation status from the registry.</summary>
    ReqnrollInstallationStatus GetInstallStatus();
}
