using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Reqnroll.IdeSupport.Common.Logging;

/// <summary>IdeSupportCompositeLogger</summary>
public class IdeSupportCompositeLogger : IIdeSupportLogger, IEnumerable<IIdeSupportLogger>
{
    private IIdeSupportLogger[] _loggers = Array.Empty<IIdeSupportLogger>();

    /// <summary>Gets the most verbose trace level among the composed loggers.</summary>
    public TraceLevel Level { get; private set; } = TraceLevel.Off;

    /// <summary>Forwards the log message to every composed logger.</summary>
    public void Log(LogMessage message)
    {
        foreach (var logger in _loggers)
            logger.Log(message);
    }

    /// <summary>Returns an enumerator over the composed loggers.</summary>
    public IEnumerator<IIdeSupportLogger> GetEnumerator() => _loggers.AsEnumerable().GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>Adds a logger to the composite and updates <see cref="Level"/> to the most verbose of all composed loggers.</summary>
    public IdeSupportCompositeLogger Add(IIdeSupportLogger logger)
    {
        _loggers = _loggers.Concat(new[] {logger}).ToArray();
        Level = _loggers.Max(l => l.Level);
        return this;
    }
}
