#nullable enable
using Reqnroll;
using System;
using System.Collections.Generic;

namespace Reqnroll.IdeSupport.Common.Telemetry;

public interface ITelemetryTransmitter
{
    void TransmitEvent(ITelemetryEvent runtimeEvent);
    void TransmitExceptionEvent(Exception exception, IEnumerable<KeyValuePair<string, object>> additionalProps);
    void TransmitFatalExceptionEvent(Exception exception, bool isFatal);
}
