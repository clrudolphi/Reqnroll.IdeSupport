#nullable enable

using System;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Reqnroll.IdeSupport.Common.Logging;

/// <summary>
/// Canonical <see cref="TraceLevel"/> &lt;-&gt; <see cref="LogLevel"/> mapping, shared by every
/// place that bridges <see cref="IIdeSupportLogger"/> (our app-level logging API) onto
/// <see cref="Microsoft.Extensions.Logging"/>'s <c>ILogger</c>/<c>ILogger&lt;T&gt;</c> abstractions.
/// </summary>
public static class IdeSupportLogLevelConverter
{
    /// <summary>Gets or sets the to trace level.</summary>
    public static TraceLevel ToTraceLevel(LogLevel level) => level switch
    {
        LogLevel.Trace or LogLevel.Debug    => TraceLevel.Verbose,
        LogLevel.Information                => TraceLevel.Info,
        LogLevel.Warning                    => TraceLevel.Warning,
        LogLevel.Error or LogLevel.Critical => TraceLevel.Error,
        _                                   => TraceLevel.Off,
    };

    /// <summary>Gets or sets the to log level.</summary>
    public static LogLevel ToLogLevel(TraceLevel level) => level switch
    {
        TraceLevel.Off     => LogLevel.None,
        TraceLevel.Error   => LogLevel.Error,
        TraceLevel.Warning => LogLevel.Warning,
        TraceLevel.Info    => LogLevel.Information,
        TraceLevel.Verbose => LogLevel.Trace,
        _                  => LogLevel.Warning,
    };
}

/// <summary>Adapts a single <see cref="Microsoft.Extensions.Logging"/> category onto an <see cref="IIdeSupportLogger"/> sink.</summary>
public sealed class IdeSupportLoggerAdapter : ILogger
{
    private readonly string _categoryName;
    private readonly IIdeSupportLogger _logger;

    /// <summary>Initializes a new instance of the <see cref="IdeSupportLoggerAdapter"/> class.</summary>
    public IdeSupportLoggerAdapter(string categoryName, IIdeSupportLogger logger)
    {
        _categoryName = categoryName;
        _logger = logger;
    }

    /// <summary>Gets or sets the is enabled.</summary>
    public bool IsEnabled(LogLevel logLevel) => _logger.IsLogging(IdeSupportLogLevelConverter.ToTraceLevel(logLevel));

    /// <summary>Writes a log entry for the specified category.</summary>
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        _logger.Log(new LogMessage(IdeSupportLogLevelConverter.ToTraceLevel(logLevel), formatter(state, exception),
            _categoryName, exception));
    }

    /// <summary>Begins a logical operation scope.</summary>
    public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

    private sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new();
        public void Dispose() { }
    }
}

/// <summary>
/// Bridges a single <see cref="IIdeSupportLogger"/> sink onto the standard
/// <see cref="ILoggerFactory"/>/<see cref="ILogger{TCategoryName}"/> abstractions, so that
/// consumers can be written against <c>ILogger&lt;T&gt;</c> while still writing through the same
/// file/debug sink as everything else using <see cref="IIdeSupportLogger"/> directly.
/// </summary>
public sealed class IdeSupportLoggerFactory : ILoggerFactory
{
    private readonly IIdeSupportLogger _logger;

    /// <summary>Initializes a new instance of the <see cref="IdeSupportLoggerFactory"/> class.</summary>
    public IdeSupportLoggerFactory(IIdeSupportLogger logger) => _logger = logger;

    /// <summary>Gets or sets the create logger.</summary>
    public ILogger CreateLogger(string categoryName) => new IdeSupportLoggerAdapter(categoryName, _logger);

    // Single sink by design - IIdeSupportLogger already fans out via IdeSupportCompositeLogger when needed.
    /// <summary>Gets or sets the add provider.</summary>
    public void AddProvider(ILoggerProvider provider) { }

    /// <summary>Gets or sets the dispose.</summary>
    public void Dispose() { }
}
