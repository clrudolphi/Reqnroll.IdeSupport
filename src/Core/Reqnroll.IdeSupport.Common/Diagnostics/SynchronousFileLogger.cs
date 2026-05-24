using System;
using System.Diagnostics;

namespace Reqnroll.IdeSupport.Common.Diagnostics;

public class SynchronousFileLogger : AsynchronousFileLogger
{
    public SynchronousFileLogger()
        : base(new FileSystemForIDE(), TraceLevel.Verbose)
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
