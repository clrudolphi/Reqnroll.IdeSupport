using Reqnroll.IdeSupport.Common.Logging;
using Reqnroll.IdeSupport.LSP.Server.Telemetry;

namespace Reqnroll.IdeSupport.LSP.Server.Tests.Telemetry;

public class FileLoggingLspTelemetryServiceTests
{
    private sealed class CapturingDebugLog : ITelemetryDebugLog
    {
        public bool IsEnabled => true;
        public List<(string Source, string Event, object? Props, bool? Enabled, bool? Transmitted, string? Error)> Records { get; } = new();

        public void Record(string source, string eventName, object? properties,
            bool? enabled = null, bool? transmitted = null, string? error = null)
            => Records.Add((source, eventName, properties, enabled, transmitted, error));
    }

    [Fact]
    public void SendEvent_mirrors_to_debug_log_and_forwards_to_inner()
    {
        var inner = Substitute.For<ILspTelemetryService>();
        var debugLog = new CapturingDebugLog();
        var sut = new FileLoggingLspTelemetryService(inner, debugLog);

        var props = new Dictionary<string, object?> { ["DiscoverySource"] = "Connector" };
        sut.SendEvent("Reqnroll Discovery executed", props);

        // Forwarded unchanged to the real emitter.
        inner.Received(1).SendEvent("Reqnroll Discovery executed", props);

        // Mirrored, tagged as server-originated, with the same properties.
        debugLog.Records.Should().ContainSingle();
        var rec = debugLog.Records[0];
        rec.Source.Should().Be("server");
        rec.Event.Should().Be("Reqnroll Discovery executed");
        rec.Props.Should().BeSameAs(props);
    }

    [Fact]
    public void SendEvent_still_forwards_when_debug_log_is_the_null_sink()
    {
        var inner = Substitute.For<ILspTelemetryService>();
        var sut = new FileLoggingLspTelemetryService(inner, NullTelemetryDebugLog.Instance);

        sut.SendEvent("X", new Dictionary<string, object?>());

        inner.Received(1).SendEvent("X", Arg.Any<Dictionary<string, object?>>());
    }
}
