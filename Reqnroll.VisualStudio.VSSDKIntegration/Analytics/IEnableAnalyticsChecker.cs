using Reqnroll.IDE.Common.Analytics;
using System;
using System.ComponentModel.Composition;
using System.Linq;

namespace Reqnroll.VisualStudio.SDKIntegration.Analytics;


[Export(typeof(IEnableAnalyticsChecker))]
public class EnableAnalyticsChecker : Reqnroll.IDE.Common.Analytics.EnableAnalyticsChecker { }
