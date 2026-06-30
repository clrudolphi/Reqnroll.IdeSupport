using System;
using System.Diagnostics;

namespace Reqnroll.IdeSupport.Common.Diagnostics;

public class SynchronousFileLogger : AsynchronousFileLogger
{
    public SynchronousFileLogger(string ide = "vs", string role = "ext")
        : base(new FileSystemForIDE(), TraceLevel.Verbose, ide, role)
    {
        EnsureLogFolder();
    }

    public override void Log(LogMessage message)
    {
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
