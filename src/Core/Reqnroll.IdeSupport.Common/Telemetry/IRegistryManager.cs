namespace Reqnroll.IdeSupport.Common.Telemetry;

/// <summary>IRegistryManager</summary>
public interface IRegistryManager
{
    /// <summary>Gets or sets the update status.</summary>
    bool UpdateStatus(ReqnrollInstallationStatus status);
    /// <summary>Gets or sets the get install status.</summary>
    ReqnrollInstallationStatus GetInstallStatus();
}
