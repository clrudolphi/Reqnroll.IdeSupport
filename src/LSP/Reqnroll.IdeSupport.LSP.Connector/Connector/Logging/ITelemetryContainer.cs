namespace ReqnrollConnector.Logging;

public interface ITelemetryContainer
{
    void AddTelemetryProperty(string key, string value);
    Dictionary<string, object> ToDictionary();
}
