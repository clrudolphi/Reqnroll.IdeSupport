#nullable enable
using Reqnroll.IDE.Common.Analytics;
using Reqnroll.VisualStudio.Diagnostics;
using System.ComponentModel.Composition;
using System.Net.Http;

namespace Reqnroll.VisualStudio.Analytics;

[Export(typeof(IAnalyticsTransmitter))]
public class AnalyticsTransmitter : Reqnroll.IDE.Common.Analytics.AnalyticsTransmitter
{

    [ImportingConstructor]
    public AnalyticsTransmitter(IAnalyticsTransmitterSink analyticsTransmitterSink,
        IEnableAnalyticsChecker enableAnalyticsChecker, DeveroomCompositeLogger? logger = null) : base(analyticsTransmitterSink, enableAnalyticsChecker, logger) { }

}
