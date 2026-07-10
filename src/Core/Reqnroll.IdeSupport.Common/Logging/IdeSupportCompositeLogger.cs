using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Reqnroll.IdeSupport.Common.Logging;

public class IdeSupportCompositeLogger : IIdeSupportLogger, IEnumerable<IIdeSupportLogger>
{
    private IIdeSupportLogger[] _loggers = Array.Empty<IIdeSupportLogger>();

    public TraceLevel Level { get; private set; } = TraceLevel.Off;

    public void Log(LogMessage message)
    {
        foreach (var logger in _loggers)
            logger.Log(message);
    }

    public IEnumerator<IIdeSupportLogger> GetEnumerator() => _loggers.AsEnumerable().GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public IdeSupportCompositeLogger Add(IIdeSupportLogger logger)
    {
        _loggers = _loggers.Concat(new[] {logger}).ToArray();
        Level = _loggers.Max(l => l.Level);
        return this;
    }
}
