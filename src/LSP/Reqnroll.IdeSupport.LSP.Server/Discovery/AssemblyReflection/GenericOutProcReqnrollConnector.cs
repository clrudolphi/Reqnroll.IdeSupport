using Reqnroll.IdeSupport.Common.Configuration;
using Reqnroll.IdeSupport.Common.Logging;
using Reqnroll.IdeSupport.Common.ProjectSystem;
using Reqnroll.IdeSupport.Common.ProjectSystem.Settings;
using Reqnroll.IdeSupport.Common.Telemetry;

namespace Reqnroll.IdeSupport.LSP.Server.Discovery;

public class GenericOutProcReqnrollConnector : OutProcReqnrollConnector
{
    private const string ConnectorNet462 = @"Reqnroll-Generic-net462\reqnroll-ide-connector.exe";
    private const string ConnectorNet472 = @"Reqnroll-Generic-net472\reqnroll-ide-connector.exe";
    private const string ConnectorNet481 = @"Reqnroll-Generic-net481\reqnroll-ide-connector.exe";
    private const string ConnectorNet60 = @"Reqnroll-Generic-net6.0\reqnroll-ide-connector.dll";
    private const string ConnectorNet70 = @"Reqnroll-Generic-net7.0\reqnroll-ide-connector.dll";
    private const string ConnectorNet80 = @"Reqnroll-Generic-net8.0\reqnroll-ide-connector.dll";
    private const string ConnectorNet90 = @"Reqnroll-Generic-net9.0\reqnroll-ide-connector.dll";
    private const string ConnectorNet100 = @"Reqnroll-Generic-net10.0\reqnroll-ide-connector.dll";

    public GenericOutProcReqnrollConnector(
        DeveroomConfiguration configuration,
        IIdeSupportLogger logger,
        TargetFrameworkMoniker targetFrameworkMoniker,
        string extensionFolder,
        ProcessorArchitectureSetting processorArchitecture,
        ProjectSettings projectSettings,
        ITelemetryService telemetryService)
        : base(
            configuration,
            logger,
            targetFrameworkMoniker,
            extensionFolder,
            processorArchitecture,
            projectSettings,
            telemetryService)
    {
    }

    protected override string GetConnectorPath(List<string> arguments)
    {
        var connector = ConnectorNet80;

        if (_targetFrameworkMoniker.IsNetFramework && _targetFrameworkMoniker.HasVersion) 
        {
            connector = _targetFrameworkMoniker.Version.Minor switch
            {
                6 => ConnectorNet462,
                7 => ConnectorNet472,
                _ => ConnectorNet481,
            };
            return Path.Combine(GetConnectorsFolder(), connector);
        }

        if (_targetFrameworkMoniker.IsNetCore && _targetFrameworkMoniker.HasVersion &&
            _targetFrameworkMoniker.Version.Major == 6)
        {
            connector = ConnectorNet60;
        }

        if (_targetFrameworkMoniker.IsNetCore && _targetFrameworkMoniker.HasVersion &&
            _targetFrameworkMoniker.Version.Major == 7)
        {
            connector = ConnectorNet70;
        }

        if (_targetFrameworkMoniker.IsNetCore && _targetFrameworkMoniker.HasVersion &&
            _targetFrameworkMoniker.Version.Major == 8)
        {
            connector = ConnectorNet80;
        }

        if (_targetFrameworkMoniker.IsNetCore && _targetFrameworkMoniker.HasVersion &&
            _targetFrameworkMoniker.Version.Major == 9)
        {
            connector = ConnectorNet90;
        }

        if (_targetFrameworkMoniker.IsNetCore && _targetFrameworkMoniker.HasVersion &&
            _targetFrameworkMoniker.Version.Major >= 10)
        {
            connector = ConnectorNet100;
        }

        var connectorsFolder = GetConnectorsFolder();
        return GetDotNetExecCommand(arguments, connectorsFolder, connector);
    }
}
