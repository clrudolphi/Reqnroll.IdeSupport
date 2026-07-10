using System.Reflection;
using Reqnroll.IdeSupport.Common.Logging;
using Reqnroll.IdeSupport.LSP.Server.Hosting;

namespace Reqnroll.IdeSupport.LSP.Server.Logging;

/// <summary>
/// <see cref="IIdeSupportLogger"/> used by the LSP server process.
/// Delegates to a <see cref="IdeSupportCompositeLogger"/> composed of:
/// <list type="bullet">
///   <item><see cref="IdeSupportDebugLogger"/> — writes to <see cref="Debug"/> output</item>
///   <item><see cref="SynchronousFileLogger"/> — appends to the Reqnroll log file</item>
/// </list>
/// Emits a session-start banner as the first log line so runs within a day-appended file
/// can be distinguished by version, PID, and server path.
/// </summary>
public sealed class LspIdeSupportLogger : IIdeSupportLogger
{
    private readonly IdeSupportCompositeLogger _inner;

    public LspIdeSupportLogger(ClientIdeContext clientIdeContext)
    {
        var idePrefix = clientIdeContext.Ide switch
        {
            "visualstudio" => "vs",
            "vscode"       => "vscode",
            _              => "lsp"   // unknown or absent --ide; avoid misattributing to a known IDE
        };
        _inner = new IdeSupportCompositeLogger()
            .Add(new IdeSupportDebugLogger())
            .Add(new SynchronousFileLogger(idePrefix, "server", clientIdeContext.LogLevel));

        var version   = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "unknown";
        var location  = Assembly.GetExecutingAssembly().Location;
        var pid       = Environment.ProcessId;
        this.LogInfo($"=== Reqnroll LSP Server started — v{version}, PID {pid}, {location} ===");
    }

    public TraceLevel Level => _inner.Level;

    public void Log(LogMessage message) => _inner.Log(message);
}
