using System.Diagnostics;

namespace Reqnroll.IdeSupport.Common.Diagnostics;

public interface IDeveroomLogger
{
    TraceLevel Level { get; }
    void Log(LogMessage message);
}
