using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Reqnroll.IdeSupport.Common.Telemetry;

namespace Reqnroll.IdeSupport.Common.Logging;

/// <summary>IdeSupportLoggerExtensions</summary>
public static class IdeSupportLoggerExtensions
{
    /// <summary>Gets or sets the is logging.</summary>
    public static bool IsLogging(this IIdeSupportLogger logger, TraceLevel messageLevel)
        => messageLevel <= logger.Level;

    /// <summary>Gets or sets the log error.</summary>
    public static void LogError(this IIdeSupportLogger logger, string message,
        [CallerMemberName] string callerName = "???")
    {
        var msg = new LogMessage(TraceLevel.Error, message, callerName);
        logger.Log(msg);
    }

    /// <summary>Gets or sets the log warning.</summary>
    public static void LogWarning(this IIdeSupportLogger logger, string message,
        [CallerMemberName] string callerName = "???")
    {
        var msg = new LogMessage(TraceLevel.Warning, message, callerName);
        logger.Log(msg);
    }

    /// <summary>Gets or sets the log info.</summary>
    public static void LogInfo(this IIdeSupportLogger logger, string message,
        [CallerMemberName] string callerName = "???")
    {
        var msg = new LogMessage(TraceLevel.Info, message, callerName);
        logger.Log(msg);
    }

    /// <summary>Gets or sets the log verbose.</summary>
    public static void LogVerbose(this IIdeSupportLogger logger, string message,
        [CallerMemberName] string callerName = "???")
    {
        var msg = new LogMessage(TraceLevel.Verbose, message, callerName);
        logger.Log(msg);
    }

    /// <summary>Gets or sets the log verbose.</summary>
    public static void LogVerbose(this IIdeSupportLogger logger, Func<string> message,
        [CallerMemberName] string callerName = "???")
    {
        if (!logger.IsLogging(TraceLevel.Verbose)) return;

        var msg = new LogMessage(TraceLevel.Verbose, message(), callerName);
        logger.Log(msg);
    }

    /// <summary>Gets or sets the log exception.</summary>
    public static void LogException(this IIdeSupportLogger logger, ITelemetryService telemetryService, Exception ex,
        string message = "Exception", [CallerMemberName] string callerName = "???")
    {
        telemetryService.MonitorError(ex);
        LogException(logger, ex, message, callerName);
    }

    /// <summary>Gets or sets the log exception.</summary>
    public static void LogException(this IIdeSupportLogger logger, Exception ex, string message = "Exception",
        [CallerMemberName] string callerName = "???")
    {
        //Debug.Fail(ex.ToString());
        var msg = new LogMessage(TraceLevel.Error, message, callerName, ex);
        logger.Log(msg);
    }

    /// <summary>Gets or sets the log verbose exception.</summary>
    public static void LogVerboseException(this IIdeSupportLogger logger, ITelemetryService telemetryService,
        Exception ex, string message = "Exception", [CallerMemberName] string callerName = "???")
    {
        telemetryService.MonitorError(ex, false);
        var msg = new LogMessage(TraceLevel.Verbose, message, callerName, ex);
        logger.Log(msg);
    }

    //TODO: merge IIdeSupportLogger with ITelemetryService
    /// <summary>Gets or sets the log debug exception.</summary>
    public static void LogDebugException(this IIdeSupportLogger logger, Exception ex, string message = "Exception",
        [CallerMemberName] string callerName = "???")
    {
        var msg = new LogMessage(TraceLevel.Verbose, message, callerName, ex);
        logger.Log(msg);
    }


    /// <summary>Gets or sets the trace.</summary>
    public static void Trace(this IIdeSupportLogger logger, Stopwatch sw, string message = "",
        [CallerFilePath] string callerFilePath = "?", [CallerLineNumber] int callerLineNumber = 0,
        [CallerMemberName] string callerName = "???")
    {
        if (sw.ElapsedMilliseconds > 10)
            Trace(logger, $"{sw.Elapsed} {message}", callerFilePath, callerLineNumber, callerName);
    }

    /// <summary>Gets or sets the trace.</summary>
    public static void Trace(this IIdeSupportLogger logger, string message = "",
        [CallerFilePath] string callerFilePath = "?", [CallerLineNumber] int callerLineNumber = 0,
        [CallerMemberName] string callerName = "???")
    {
        logger.LogVerbose($"{message} in {callerFilePath}: line {callerLineNumber}", callerName);
    }
}
