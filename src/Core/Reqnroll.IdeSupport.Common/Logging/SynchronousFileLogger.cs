using System;
using System.Diagnostics;

namespace Reqnroll.IdeSupport.Common.Logging;

/// <summary>SynchronousFileLogger</summary>
public class SynchronousFileLogger : AsynchronousFileLogger
{
    /// <summary>Initializes a new instance of the <see cref="SynchronousFileLogger"/> class.</summary>
    public SynchronousFileLogger(string ide = "vs", string role = "ext", TraceLevel level = TraceLevel.Warning)
        : base(new FileSystemForIDE(), level, ide, role)
    {
        EnsureLogFolder();
    }

    /// <summary>Writes the log message to the file synchronously, swallowing any write errors.</summary>
    public override void Log(LogMessage message)
    {
        if (message.Level > Level) return;

        try
        {
            WriteLogMessage(message);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex, $"Error writing to the {LogFilePath}");
        }
    }
}
