#nullable disable
using Reqnroll;
using Reqnroll.IDE.Common.Analytics;
using System.Collections.Generic;
using System.ComponentModel.Composition;

namespace Reqnroll.VisualStudio.SDKIntegration.Analytics;

[Export(typeof(IGuidanceConfiguration))]
public class GuidanceConfiguration : Reqnroll.IDE.Common.Analytics.GuidanceConfiguration
{

}
