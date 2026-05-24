#nullable enable
using Reqnroll;
using System;
using System.Collections.Generic;

namespace Reqnroll.IdeSupport.Common.Analytics;

public interface IAnalyticsTransmitterSink
{
    void TransmitEvent(IAnalyticsEvent analyticsEvent);
    void TransmitException(Exception exception, IEnumerable<KeyValuePair<string, object>> eventName);
}
