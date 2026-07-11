using System.Diagnostics;

namespace Reqnroll.IdeSupport.Common.Logging;

/// <summary>IdeSupportNullLogger</summary>
public class IdeSupportNullLogger : IIdeSupportLogger
{
    /// <summary>Gets the trace level, always <see cref="TraceLevel.Off"/> for this no-op logger.</summary>
    public TraceLevel Level { get; } = TraceLevel.Off;

    /// <summary>No-op: discards the log message.</summary>
    public void Log(LogMessage message)
    {
        //nop;
    }
}
