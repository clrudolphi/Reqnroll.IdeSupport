using System.Diagnostics;

namespace Reqnroll.IdeSupport.Common.Logging;

/// <summary>IIdeSupportLogger</summary>
public interface IIdeSupportLogger
{
    /// <summary>Gets or sets the level.</summary>
    TraceLevel Level { get; }
    /// <summary>Gets or sets the log.</summary>
    void Log(LogMessage message);
}
