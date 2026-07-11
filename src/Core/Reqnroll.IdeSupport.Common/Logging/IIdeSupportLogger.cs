using System.Diagnostics;

namespace Reqnroll.IdeSupport.Common.Logging;

/// <summary>IIdeSupportLogger</summary>
public interface IIdeSupportLogger
{
    /// <summary>Gets the minimum trace level that will be recorded by this logger.</summary>
    TraceLevel Level { get; }
    /// <summary>Records the given log message.</summary>
    void Log(LogMessage message);
}
