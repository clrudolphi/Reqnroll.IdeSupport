#nullable enable
using Reqnroll;
using System;
using System.Collections.Generic;

namespace Reqnroll.IDE.Common.Analytics;

public interface IAnalyticsTransmitterSink
{
    void TransmitEvent(IAnalyticsEvent analyticsEvent);
    void TransmitException(Exception exception, IEnumerable<KeyValuePair<string, object>> eventName);
}
