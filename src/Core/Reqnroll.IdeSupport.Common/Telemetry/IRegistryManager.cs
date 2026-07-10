namespace Reqnroll.IdeSupport.Common.Telemetry;

public interface IRegistryManager
{
    bool UpdateStatus(ReqnrollInstallationStatus status);
    ReqnrollInstallationStatus GetInstallStatus();
}
