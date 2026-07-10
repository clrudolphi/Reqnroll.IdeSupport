using System.Diagnostics;

namespace ReqnrollConnector.Logging;

[DebuggerDisplay("{_telemetryProperties}")]
public class TelemetryContainer : ITelemetryContainer
{
    private readonly Dictionary<string, string> _telemetryProperties = new();

    public void AddTelemetryProperty(string key, string value)
    {
        _telemetryProperties.Add(key, value);
    }

    public Dictionary<string, object> ToDictionary() => _telemetryProperties.ToDictionary(e => e.Key, e => (object)e.Value);
}
