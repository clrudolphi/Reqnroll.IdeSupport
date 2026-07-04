#nullable enable

using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Reqnroll.IdeSupport.Common.Diagnostics;

namespace Reqnroll.IdeSupport.LSP.Server.Logging;

/// <summary>
/// Bridges OmniSharp's own internal <see cref="Microsoft.Extensions.Logging"/> diagnostics
/// (request dispatch, DryIoc, JSON-RPC plumbing — whatever the library itself logs via
/// <c>ILogger&lt;T&gt;</c>) into a dedicated Reqnroll log file, in addition to the existing
/// <c>window/logMessage</c> notification (<c>AddLanguageProtocolLogging()</c>) that only reaches
/// the connected client.
/// </summary>
/// <remarks>
/// Without this, OmniSharp-internal diagnostics exist only as a transient client notification —
/// if the user doesn't think to copy their editor's Output panel, that information is gone by the
/// time we're asked to look at a bug report. Writes to a separate <c>reqnroll-*-protocol-*.log</c>
/// file (not the main server log) so two independent writers never append to the same file
/// concurrently, and gated by its own <c>--protocol-log-level</c> rather than the main
/// <c>--log-level</c> — the two are deliberately decoupled (F41 follow-up): a user turning up file
/// logging for our own app-level diagnosis shouldn't also flood their editor's Output panel with
/// library internals, and vice versa.
/// </remarks>
public sealed class ProtocolLoggerProvider : ILoggerProvider
{
    private readonly IDeveroomLogger _logger;

    public ProtocolLoggerProvider(string? clientIde, TraceLevel protocolLogLevel)
        : this(BuildDefaultLogger(clientIde, protocolLogLevel))
    {
    }

    /// <summary>Test seam: bypasses the real file/debug sinks.</summary>
    internal ProtocolLoggerProvider(IDeveroomLogger logger)
    {
        _logger = logger;
    }

    private static IDeveroomLogger BuildDefaultLogger(string? clientIde, TraceLevel protocolLogLevel)
    {
        var idePrefix = clientIde switch
        {
            "visualstudio" => "vs",
            "vscode"       => "vscode",
            _              => "lsp"
        };
        return new DeveroomCompositeLogger()
            .Add(new DeveroomDebugLogger())
            .Add(new SynchronousFileLogger(idePrefix, "protocol", protocolLogLevel));
    }

    public ILogger CreateLogger(string categoryName) => new ProtocolLoggerAdapter(categoryName, _logger);

    public void Dispose()
    {
    }
}

/// <summary>Adapts a single <see cref="Microsoft.Extensions.Logging"/> category onto <see cref="IDeveroomLogger"/>.</summary>
internal sealed class ProtocolLoggerAdapter : ILogger
{
    private readonly string _categoryName;
    private readonly IDeveroomLogger _logger;

    public ProtocolLoggerAdapter(string categoryName, IDeveroomLogger logger)
    {
        _categoryName = categoryName;
        _logger = logger;
    }

    // The .NET logging pipeline already applies logging.SetMinimumLevel(...) before this
    // method is ever invoked, so no further filtering is needed here — only the
    // IDeveroomLogger sink's own level (protocolLogLevel) applies from this point on.
    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        _logger.Log(new LogMessage(ToTraceLevel(logLevel), formatter(state, exception), _categoryName, exception));
    }

    public IDisposable BeginScope<TState>(TState state) => NullScope.Instance;

    private sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new();
        public void Dispose() { }
    }

    internal static TraceLevel ToTraceLevel(LogLevel level) => level switch
    {
        LogLevel.Trace or LogLevel.Debug => TraceLevel.Verbose,
        LogLevel.Information             => TraceLevel.Info,
        LogLevel.Warning                 => TraceLevel.Warning,
        LogLevel.Error or LogLevel.Critical => TraceLevel.Error,
        _                                 => TraceLevel.Off,
    };
}
