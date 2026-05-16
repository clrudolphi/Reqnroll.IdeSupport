namespace Reqnroll.IDE.Common.Analytics;

public interface IRegistryManager
{
    bool UpdateStatus(ReqnrollInstallationStatus status);
    ReqnrollInstallationStatus GetInstallStatus();
}
