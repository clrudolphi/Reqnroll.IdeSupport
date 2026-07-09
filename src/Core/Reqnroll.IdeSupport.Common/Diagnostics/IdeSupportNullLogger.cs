using System.Diagnostics;

namespace Reqnroll.IdeSupport.Common.Diagnostics;

public class IdeSupportNullLogger : IIdeSupportLogger
{
    public TraceLevel Level { get; } = TraceLevel.Off;

    public void Log(LogMessage message)
    {
        //nop;
    }
}
