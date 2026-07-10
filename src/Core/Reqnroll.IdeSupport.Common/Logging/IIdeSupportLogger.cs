using System.Diagnostics;

namespace Reqnroll.IdeSupport.Common.Logging;

public interface IIdeSupportLogger
{
    TraceLevel Level { get; }
    void Log(LogMessage message);
}
