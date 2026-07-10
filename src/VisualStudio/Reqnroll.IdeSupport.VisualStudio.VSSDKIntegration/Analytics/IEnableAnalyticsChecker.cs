using System.ComponentModel.Composition;
using Reqnroll.IdeSupport.Common.Telemetry;

namespace Reqnroll.IdeSupport.VisualStudio.SDKIntegration.Analytics;


[Export(typeof(IEnableTelemetryChecker))]
public class EnableAnalyticsChecker : Reqnroll.IdeSupport.Common.Telemetry.EnableAnalyticsChecker { }
