using System;
using System.Collections.Generic;

namespace Reqnroll.IdeSupport.Common.Telemetry;

/// <summary>ITelemetryTransmitter</summary>
public interface ITelemetryTransmitter
{
    /// <summary>Transmits a fully-formed telemetry event.</summary>
    void TransmitEvent(ITelemetryEvent runtimeEvent);
    /// <summary>Transmits an event describing the given exception, with additional properties.</summary>
    void TransmitExceptionEvent(Exception exception, IEnumerable<KeyValuePair<string, object>> additionalProps);
    /// <summary>Transmits an event describing the given exception, flagging whether it was fatal.</summary>
    void TransmitFatalExceptionEvent(Exception exception, bool isFatal);
}
