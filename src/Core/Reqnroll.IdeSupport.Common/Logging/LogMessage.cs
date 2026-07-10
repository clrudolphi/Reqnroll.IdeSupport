using System;
using System.Diagnostics;
using System.Threading;

namespace Reqnroll.IdeSupport.Common.Logging;

[DebuggerDisplay("{TimeStamp} {CallerMethod} {Message}")]
/// <summary>Initializes a new instance of the <see cref="LogMessage"/> class.</summary>
/// <summary>LogMessage</summary>
public record LogMessage(
    /// <summary>Gets or sets the level.</summary>
    TraceLevel Level,
    /// <summary>Gets or sets the message.</summary>
    string Message,
    /// <summary>Gets or sets the caller method.</summary>
    string CallerMethod,
    /// <summary>Gets or sets the exception.</summary>
    Exception? Exception = default!)
{
    /// <summary>Gets or sets the time stamp.</summary>
    public DateTimeOffset TimeStamp { get; } = DateTimeOffset.Now;
    /// <summary>Gets or sets the managed thread id.</summary>
    public int ManagedThreadId { get; } = Thread.CurrentThread.ManagedThreadId;
}
