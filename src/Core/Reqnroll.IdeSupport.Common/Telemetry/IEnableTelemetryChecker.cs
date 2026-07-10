using System;

namespace Reqnroll.IdeSupport.Common.Telemetry;

/// <summary>IEnableTelemetryChecker</summary>
public interface IEnableTelemetryChecker
{
    /// <summary>Gets or sets the is enabled.</summary>
    bool IsEnabled();
}

/// <summary>EnableTelemetryChecker</summary>
public class EnableTelemetryChecker : IEnableTelemetryChecker
{
    /// <summary>Gets or sets the reqnroll telemetry environment variable.</summary>
    public const string ReqnrollTelemetryEnvironmentVariable = "REQNROLL_TELEMETRY_ENABLED";

    /// <summary>Gets or sets the is enabled.</summary>
    public bool IsEnabled()
    {
        var reqnrollTelemetry = Environment.GetEnvironmentVariable(ReqnrollTelemetryEnvironmentVariable);
        return reqnrollTelemetry == null || reqnrollTelemetry.Equals("1");
    }
}
