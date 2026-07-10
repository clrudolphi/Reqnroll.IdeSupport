#nullable disable
using Reqnroll;
using Reqnroll.IdeSupport.Common.Telemetry;
using System.Collections.Generic;
using System.ComponentModel.Composition;

namespace Reqnroll.IdeSupport.VisualStudio.SDKIntegration.Telemetry;

[Export(typeof(IGuidanceConfiguration))]
public class GuidanceConfiguration : Reqnroll.IdeSupport.Common.Telemetry.GuidanceConfiguration
{

}
