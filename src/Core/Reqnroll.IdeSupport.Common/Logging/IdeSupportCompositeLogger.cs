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

    /// <summary>Gets or sets the level.</summary>
    public TraceLevel Level { get; private set; } = TraceLevel.Off;

    /// <summary>Gets or sets the log.</summary>
    public void Log(LogMessage message)
    {
        foreach (var logger in _loggers)
            logger.Log(message);
    }

    /// <summary>Gets or sets the get enumerator.</summary>
    public IEnumerator<IIdeSupportLogger> GetEnumerator() => _loggers.AsEnumerable().GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>Gets or sets the add.</summary>
    public IdeSupportCompositeLogger Add(IIdeSupportLogger logger)
    {
        _loggers = _loggers.Concat(new[] {logger}).ToArray();
        Level = _loggers.Max(l => l.Level);
        return this;
    }
}
