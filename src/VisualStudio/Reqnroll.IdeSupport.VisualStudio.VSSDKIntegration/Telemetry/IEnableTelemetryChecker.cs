using System.ComponentModel.Composition;
using Reqnroll.IdeSupport.Common.Telemetry;

namespace Reqnroll.IdeSupport.VisualStudio.SDKIntegration.Telemetry;


[Export(typeof(IEnableTelemetryChecker))]
public class EnableTelemetryChecker : Reqnroll.IdeSupport.Common.Telemetry.EnableTelemetryChecker { }
