using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Reqnroll.IdeSupport.Common.Logging;

/// <summary>AsynchronousFileLogger</summary>
public class AsynchronousFileLogger : IIdeSupportLogger, IDisposable
{
    private readonly Channel<LogMessage> _channel;
    private readonly IFileSystemForIDE _fileSystem;
    private readonly CancellationTokenSource _stopTokenSource;

    /// <summary>Initializes a new instance of the <see cref="AsynchronousFileLogger"/> class.</summary>
    protected AsynchronousFileLogger(IFileSystemForIDE fileSystem, TraceLevel level, string ide, string role)
    {
        _fileSystem = fileSystem;
        Level = ApplyDebugEnvironmentOverride(level);
        // Unbounded so that bursts (e.g. 30+ concurrent spec scenarios) never silently drop messages.
        _channel = Channel.CreateUnbounded<LogMessage>();
        _stopTokenSource = new CancellationTokenSource();
        LogFilePath = GetLogFile(ide, role);
    }

    /// <summary>Gets or sets the log file path.</summary>
    public string LogFilePath { get; private set; }
    /// <summary>Gets the minimum trace level that will be written to the log.</summary>
    public TraceLevel Level { get; }

    // Matches the legacy Reqnroll.VisualStudio DeveroomDebugLogger behavior: REQNROLLVS_DEBUG=1/true
    // forces Verbose, any other value that parses as a TraceLevel name overrides the configured level.
    private static TraceLevel ApplyDebugEnvironmentOverride(TraceLevel level)
    {
        var env = Environment.GetEnvironmentVariable("REQNROLLVS_DEBUG");
        if (env == null) return level;

        if (env.Equals("1") || env.Equals("true", StringComparison.InvariantCultureIgnoreCase))
            return TraceLevel.Verbose;

        return Enum.TryParse<TraceLevel>(env, true, out var envLevel) ? envLevel : level;
    }

    /// <summary>Queues a log message for asynchronous writing, if its level is within the configured threshold.</summary>
    public virtual void Log(LogMessage message)
    {
        if (message.Level > Level) return;
        _channel.Writer.TryWrite(message);
    }

    /// <summary>Stops the background writer and releases resources held by this logger.</summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    internal static string GetLogFile(string ide, string role)
    {
        // PID is included so that concurrent instances (e.g. two VS windows, each with its own
        // extension host and LSP server process) never append to the same log file.
        var pid = Process.GetCurrentProcess().Id;
        return Path.Combine(Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData), "Reqnroll",
#if DEBUG
            $"reqnroll-{ide}-{role}-debug-{DateTime.UtcNow:yyyyMMdd}-{pid}.log");
#else
            $"reqnroll-{ide}-{role}-{DateTime.Now:yyyyMMdd}-{pid}.log");
#endif
    }

    /// <summary>Creates a new logger and starts its background log-writing task.</summary>
    public static AsynchronousFileLogger CreateInstance(IFileSystemForIDE fileSystem, string ide, string role)
    {
        var fileLogger = new AsynchronousFileLogger(fileSystem, TraceLevel.Verbose, ide, role);
        Task.Factory.StartNew(
            fileLogger.Start,
            fileLogger._stopTokenSource.Token,
            TaskCreationOptions.LongRunning,
            TaskScheduler.Default);
        return fileLogger;
    }

    private Task Start()
    {
        EnsureLogFolder();
        DeleteOldLogFiles();
        return WorkerLoop();
    }

    private async Task WorkerLoop()
    {
        while (!_stopTokenSource.IsCancellationRequested)
            try
            {
                var message = await _channel.Reader.ReadAsync(_stopTokenSource.Token);
                WriteLogMessage(message);
            }
            catch (Exception ex) when (ex is not (ChannelClosedException or TaskCanceledException))
            {
                Debug.WriteLine(ex, $"Error writing to the {LogFilePath}");
            }
            catch
            {
                // ignored
            }
    }

    /// <summary>Formats a log message and appends it to the log file.</summary>
    protected void WriteLogMessage(LogMessage message)
    {
        // Indent continuation lines so multi-line messages (e.g. connector JSON, stack traces)
        // remain visually grouped without losing the structured prefix on the first line.
        var body = message.Message.Replace("\r\n", "\n").Replace("\n", "\n    ");
        var content =
            $"{message.TimeStamp:yyyy-MM-ddTHH\\:mm\\:ss.fffzzz}, {message.Level}@{message.ManagedThreadId}, {message.CallerMethod}: {body}";
        if (message.Exception != null) content += $"\n    : {message.Exception}".Replace("\n", "\n    ");
        content += Environment.NewLine;

        _fileSystem.File.AppendAllText(LogFilePath, content, Encoding.UTF8);
    }

    /// <summary>Resolves <see cref="LogFilePath"/> to a full path and creates its containing folder if missing.</summary>
    protected void EnsureLogFolder()
    {
        LogFilePath = Path.GetFullPath(LogFilePath);
        var logFolder = Path.GetDirectoryName(LogFilePath);
        if (!_fileSystem.Directory.Exists(logFolder))
            _fileSystem.Directory.CreateDirectory(logFolder);
    }

    private void DeleteOldLogFiles()
    {
        try
        {
            var logFolder = Path.GetDirectoryName(LogFilePath);
            if (!Directory.Exists(logFolder))
                return;

            var logFiles = Directory.GetFiles(logFolder, "reqnroll-*.log");

            foreach (string logFile in logFiles)
            {
                FileInfo fi = new FileInfo(logFile);
                if (fi.LastWriteTime < DateTime.UtcNow.AddDays(-10))
                    fi.Delete();
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex, "Error deleting log files");
        }
    }

    /// <summary>Stops the background writer and releases the channel and cancellation token when <paramref name="disposing"/> is true.</summary>
    protected virtual void Dispose(bool disposing)
    {
        if (!disposing) return;

        _channel.Writer.TryComplete();
        _stopTokenSource.Cancel(true);
        _stopTokenSource.Dispose();
    }
}
