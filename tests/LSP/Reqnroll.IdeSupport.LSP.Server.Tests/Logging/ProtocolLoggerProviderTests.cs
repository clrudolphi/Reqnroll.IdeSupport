#nullable enable

using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Reqnroll.IdeSupport.Common.Diagnostics;
using Reqnroll.IdeSupport.LSP.Server.Logging;

namespace Reqnroll.IdeSupport.LSP.Server.Tests.Logging;

public class ProtocolLoggerProviderTests
{
    private sealed class CapturingLogger : IDeveroomLogger
    {
        public TraceLevel Level => TraceLevel.Verbose;
        public List<LogMessage> Messages { get; } = new();
        public void Log(LogMessage message) => Messages.Add(message);
    }

    [Fact]
    public void CreateLogger_returns_a_logger_that_forwards_to_the_underlying_IDeveroomLogger()
    {
        var captured = new CapturingLogger();
        var provider = new ProtocolLoggerProvider(captured);

        var logger = provider.CreateLogger("OmniSharp.Extensions.LanguageServer.Server.LanguageServer");
        logger.Log(LogLevel.Information, new EventId(0), "state", null, (s, _) => "handling request");

        var message = captured.Messages.Should().ContainSingle().Subject;
        message.Message.Should().Be("handling request");
        message.Level.Should().Be(TraceLevel.Info);
        message.CallerMethod.Should().Be("OmniSharp.Extensions.LanguageServer.Server.LanguageServer");
        message.Exception.Should().BeNull();
    }

    [Fact]
    public void CreateLogger_forwards_the_exception()
    {
        var captured = new CapturingLogger();
        var provider = new ProtocolLoggerProvider(captured);
        var ex = new InvalidOperationException("boom");

        var logger = provider.CreateLogger("Category");
        logger.Log(LogLevel.Error, new EventId(0), "state", ex, (s, e) => "failed");

        captured.Messages.Should().ContainSingle().Which.Exception.Should().BeSameAs(ex);
    }

    [Fact]
    public void IsEnabled_is_always_true_because_SetMinimumLevel_already_filtered_upstream()
    {
        var adapter = new ProtocolLoggerAdapter("cat", new CapturingLogger());

        adapter.IsEnabled(LogLevel.Trace).Should().BeTrue();
        adapter.IsEnabled(LogLevel.Critical).Should().BeTrue();
    }

    [Fact]
    public void BeginScope_returns_a_disposable_that_does_not_throw()
    {
        var adapter = new ProtocolLoggerAdapter("cat", new CapturingLogger());

        var scope = adapter.BeginScope("state");

        scope.Should().NotBeNull();
        var act = () => scope.Dispose();
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(LogLevel.Trace, TraceLevel.Verbose)]
    [InlineData(LogLevel.Debug, TraceLevel.Verbose)]
    [InlineData(LogLevel.Information, TraceLevel.Info)]
    [InlineData(LogLevel.Warning, TraceLevel.Warning)]
    [InlineData(LogLevel.Error, TraceLevel.Error)]
    [InlineData(LogLevel.Critical, TraceLevel.Error)]
    [InlineData(LogLevel.None, TraceLevel.Off)]
    public void ToTraceLevel_maps_each_LogLevel(LogLevel logLevel, TraceLevel expected)
    {
        ProtocolLoggerAdapter.ToTraceLevel(logLevel).Should().Be(expected);
    }
}
