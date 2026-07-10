using System;
using System.Collections.Generic;

namespace Reqnroll.IdeSupport.Common.Telemetry;

/// <summary>ITelemetryTransmitter</summary>
public interface ITelemetryTransmitter
{
    /// <summary>Gets or sets the transmit event.</summary>
    void TransmitEvent(ITelemetryEvent runtimeEvent);
    /// <summary>Gets or sets the transmit exception event.</summary>
    void TransmitExceptionEvent(Exception exception, IEnumerable<KeyValuePair<string, object>> additionalProps);
    /// <summary>Gets or sets the transmit fatal exception event.</summary>
    void TransmitFatalExceptionEvent(Exception exception, bool isFatal);
}
