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

    /// <summary>Gets or sets the get debugger timeout.</summary>
    public static TimeSpan GetDebuggerTimeout(TimeSpan nonDebuggerTimeout)
        => Debugger.IsAttached ? TimeSpan.FromMinutes(1) : nonDebuggerTimeout;
}
