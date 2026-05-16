using System.Diagnostics;

namespace Reqnroll.IDE.Common.Diagnostics;

public class DeveroomNullLogger : IDeveroomLogger
{
    public TraceLevel Level { get; } = TraceLevel.Off;

    public void Log(LogMessage message)
    {
        //nop;
    }
}
