using System;
using System.Diagnostics;
using System.Threading;

namespace Reqnroll.IdeSupport.Common;

/// <summary>DebuggableCancellationTokenSource</summary>
public class DebuggableCancellationTokenSource : CancellationTokenSource
{
    /// <summary>
    ///     Do not forget to Dispose!
    /// </summary>
    /// <param name="nonDebuggerTimeout"></param>
    public DebuggableCancellationTokenSource(TimeSpan nonDebuggerTimeout)
        : base(GetDebuggerTimeout(nonDebuggerTimeout))
    {
    }

    /// <summary>Returns a one-minute timeout when a debugger is attached, otherwise <paramref name="nonDebuggerTimeout"/>.</summary>
    public static TimeSpan GetDebuggerTimeout(TimeSpan nonDebuggerTimeout)
        => Debugger.IsAttached ? TimeSpan.FromMinutes(1) : nonDebuggerTimeout;
}
