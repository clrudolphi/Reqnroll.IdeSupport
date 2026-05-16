#nullable enable
using Reqnroll;
using System;
using System.Collections.Generic;

namespace Reqnroll.IDE.Common.Analytics;

public interface IAnalyticsTransmitter
{
    void TransmitEvent(IAnalyticsEvent runtimeEvent);
    void TransmitExceptionEvent(Exception exception, IEnumerable<KeyValuePair<string, object>> additionalProps);
    void TransmitFatalExceptionEvent(Exception exception, bool isFatal);
}
