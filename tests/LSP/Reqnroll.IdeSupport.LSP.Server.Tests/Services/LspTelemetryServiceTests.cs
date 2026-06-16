#nullable enable

using System.Reflection;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using Reqnroll.IdeSupport.LSP.Server.Services;

namespace Reqnroll.IdeSupport.LSP.Server.Tests.Services;

public class LspTelemetryServiceTests
{
    private readonly ILanguageServerFacade _languageServer = Substitute.For<ILanguageServerFacade>();

    private LspTelemetryService CreateSut() => new(_languageServer);

    [Fact]
    public void SendEvent_calls_SendNotification_with_telemetry_event_method()
    {
        var sut = CreateSut();

        sut.SendEvent("TestEvent", new Dictionary<string, object?>());

        _languageServer.Received(1).SendNotification("telemetry/event", Arg.Any<object>());
    }

    [Fact]
    public void SendEvent_includes_eventName_and_properties_in_params()
    {
        var sut = CreateSut();
        var properties = new Dictionary<string, object?>
        {
            ["Key1"] = "value1",
            ["Count"] = 42,
        };

        sut.SendEvent("MyEvent", properties);

        _languageServer.Received(1).SendNotification(
            "telemetry/event",
            Arg.Is<object>(o => HasEventName(o, "MyEvent") && HasProperty(o, "Key1", "value1") && HasProperty(o, "Count", 42)));
    }

    private static bool HasEventName(object obj, string expectedName)
    {
        var eventName = obj.GetType().GetProperty("eventName")?.GetValue(obj) as string;
        return eventName == expectedName;
    }

    private static bool HasProperty(object obj, string key, object expectedValue)
    {
        var props = obj.GetType().GetProperty("properties")?.GetValue(obj);
        if (props is not Dictionary<string, object?> dict)
            return false;
        return dict.TryGetValue(key, out var val) && val?.Equals(expectedValue) == true;
    }
}
