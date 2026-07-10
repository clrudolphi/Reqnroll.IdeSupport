using Reqnroll.IdeSupport.Common;
using Reqnroll.IdeSupport.Common.Configuration;
using Reqnroll.IdeSupport.Common.Logging;
using Reqnroll.IdeSupport.Common.ProjectSystem;
using Reqnroll.IdeSupport.Common.ProjectSystem.Settings;
using Reqnroll.IdeSupport.Common.Telemetry;

namespace Reqnroll.IdeSupport.LSP.Server.Discovery;

public class CustomOutProcReqnrollConnector : OutProcReqnrollConnector
{
    public CustomOutProcReqnrollConnector(DeveroomConfiguration configuration, IIdeSupportLogger logger, TargetFrameworkMoniker targetFrameworkMoniker, string extensionFolder, ProcessorArchitectureSetting processorArchitecture, ProjectSettings projectSettings, ITelemetryService telemetryService) : base(configuration, logger, targetFrameworkMoniker, extensionFolder, processorArchitecture, projectSettings, telemetryService)
    {
    }

    protected override string GetConnectorPath(List<string> arguments)
    {
        var connectorPath = Path.Combine(GetConnectorsFolder(), Environment.ExpandEnvironmentVariables(_configuration.BindingDiscovery.ConnectorPath ?? "<not specified>"));

        if (".dll".Equals(Path.GetExtension(connectorPath), StringComparison.OrdinalIgnoreCase))
            connectorPath = GetDotNetExecCommand(arguments, Path.GetDirectoryName(connectorPath), Path.GetFileName(connectorPath));

        return connectorPath;
    }
}