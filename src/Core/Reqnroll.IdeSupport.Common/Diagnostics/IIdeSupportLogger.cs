using System.Diagnostics;

namespace Reqnroll.IdeSupport.Common.Diagnostics;

public interface IIdeSupportLogger
{
    TraceLevel Level { get; }
    void Log(LogMessage message);
}
