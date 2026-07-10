using System.ComponentModel.Composition;
using Reqnroll.IdeSupport.Common.Telemetry;

namespace Reqnroll.IdeSupport.VisualStudio.SDKIntegration.Telemetry;

[Export(typeof(IGuidanceConfiguration))]
public class GuidanceConfiguration : Reqnroll.IdeSupport.Common.Telemetry.GuidanceConfiguration
{

}
