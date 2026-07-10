using System;
using System.Linq;

namespace Reqnroll.IdeSupport.Common.Telemetry;

public interface IEnableTelemetryChecker
{
    bool IsEnabled();
}

public class EnableAnalyticsChecker : IEnableTelemetryChecker
{
    public const string ReqnrollTelemetryEnvironmentVariable = "REQNROLL_TELEMETRY_ENABLED";

    public bool IsEnabled()
    {
        var reqnrollTelemetry = Environment.GetEnvironmentVariable(ReqnrollTelemetryEnvironmentVariable);
        return reqnrollTelemetry == null || reqnrollTelemetry.Equals("1");
    }
}
