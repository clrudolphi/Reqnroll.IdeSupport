using Reqnroll.IDE.Common.Diagnostics;
using System.Diagnostics;

namespace Reqnroll.IDE.Common.Diagnostics;

public interface IDeveroomLogger
{
    TraceLevel Level { get; }
    void Log(LogMessage message);
}
