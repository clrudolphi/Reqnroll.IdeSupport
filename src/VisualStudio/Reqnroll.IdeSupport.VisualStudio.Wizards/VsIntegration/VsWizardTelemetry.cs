// VsIntegration layer — VS SDK references are expected here.
// Adapts the full ITelemetryService to the narrow IWizardTelemetry surface.
using Reqnroll.IdeSupport.Common.ProjectSystem;
using Reqnroll.IdeSupport.Common.ProjectSystem.Settings;
using Reqnroll.IdeSupport.VisualStudio.Wizards.Abstractions;
using OriginalTelemetryService = Reqnroll.IdeSupport.Common.Telemetry.ITelemetryService;
using OriginalProjectSettings = Reqnroll.IdeSupport.Common.ProjectSystem.Settings.ProjectSettings;

namespace Reqnroll.IdeSupport.VisualStudio.Wizards.VsIntegration;

/// <summary>
/// Adapts ITelemetryService to IWizardTelemetry. Also implements
/// IWizardTelemetryLogger for use by SafeDispatcherTimer.
/// </summary>
public class VsWizardTelemetry : IWizardTelemetry, IWizardTelemetryLogger
{
    private readonly OriginalTelemetryService _telemetryService;

    public VsWizardTelemetry(OriginalTelemetryService telemetryService)
    {
        _telemetryService = telemetryService;
    }

    public void OnFeatureFileAdded(WizardProjectSettings settings) =>
        _telemetryService.MonitorCommandAddFeatureFile(MapSettings(settings));

    public void OnConfigFileAdded(WizardProjectSettings settings) =>
        _telemetryService.MonitorCommandAddReqnrollConfigFile(MapSettings(settings));

    public void OnProjectTemplateWizardStarted() =>
        _telemetryService.MonitorProjectTemplateWizardStarted();

    public void OnProjectTemplateWizardCompleted(string dotNetFramework, string unitTestFramework) =>
        _telemetryService.MonitorProjectTemplateWizardCompleted(dotNetFramework, unitTestFramework, false);

    public void MonitorError(Exception exception, bool? isFatal = null) =>
        _telemetryService.MonitorError(exception, isFatal);

    // OriginalProjectSettings is a record — we construct a minimal stub
    // just to satisfy the ITelemetryService signatures that expect it.
    // TODO: Once MonitorCommandAdd* is refactored to accept a plain label
    // string this adapter can be simplified.
    private static OriginalProjectSettings MapSettings(WizardProjectSettings wps)
        {
        var kind = wps.IsReqnrollProject || wps.IsSpecFlowProject ? 
                    DeveroomProjectKind.ReqnrollTestProject : DeveroomProjectKind.Unknown;
        var reqnrollVersion = wps.ReqnrollVersionLabel is not null ? 
                new NuGetVersion(wps.ReqnrollVersionLabel, null) : new NuGetVersion("0.0.0", null);
        var traits = ReqnrollProjectTraits.None;
        if ( wps.HasXUnitAdapter)
        {
            traits = traits | ReqnrollProjectTraits.XUnitAdapter;
        }
        return new OriginalProjectSettings(
                kind,
                null!, null!, default, null!, null!, reqnrollVersion, null!, null!, traits, default);
   
    }

}

