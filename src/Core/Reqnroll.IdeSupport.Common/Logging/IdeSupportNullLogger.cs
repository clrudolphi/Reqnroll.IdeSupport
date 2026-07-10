using System.Diagnostics;

namespace Reqnroll.IdeSupport.Common.Logging;

/// <summary>IdeSupportNullLogger</summary>
public class IdeSupportNullLogger : IIdeSupportLogger
{
    /// <summary>Gets or sets the level.</summary>
    public TraceLevel Level { get; } = TraceLevel.Off;

    /// <summary>Gets or sets the log.</summary>
    public void Log(LogMessage message)
    {
        //nop;
    }
}
