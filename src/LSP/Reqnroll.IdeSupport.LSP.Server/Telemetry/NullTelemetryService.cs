#nullable disable
using Reqnroll.IdeSupport.Common;
using Reqnroll.IdeSupport.Common.Telemetry;
using Reqnroll.IdeSupport.Common.ProjectSystem.Settings;

namespace Reqnroll.IdeSupport.LSP.Server.Telemetry;

/// <summary>
/// No-op <see cref="ITelemetryService"/> for the LSP server process.
/// Telemetry is not collected from the server side.
/// </summary>
public sealed class NullTelemetryService : ITelemetryService
{
    /// <summary>Gets or sets the instance.</summary>
    public static readonly NullTelemetryService Instance = new();

    private NullTelemetryService() { }

    /// <summary>Gets or sets the monitor load project system.</summary>
    public void MonitorLoadProjectSystem() { }
    /// <summary>Gets or sets the monitor open project system.</summary>
    public void MonitorOpenProjectSystem(IIdeScope ideScope) { }
    /// <summary>Gets or sets the monitor open project.</summary>
    public void MonitorOpenProject(ProjectSettings settings, int? featureFileCount) { }
    /// <summary>Gets or sets the monitor open feature file.</summary>
    public void MonitorOpenFeatureFile(ProjectSettings projectSettings) { }
    /// <summary>Gets or sets the monitor extension installed.</summary>
    public void MonitorExtensionInstalled() { }
    /// <summary>Gets or sets the monitor extension upgraded.</summary>
    public void MonitorExtensionUpgraded(string oldExtensionVersion) { }
    /// <summary>Gets or sets the monitor extension days of usage.</summary>
    public void MonitorExtensionDaysOfUsage(int usageDays) { }
    /// <summary>Gets or sets the monitor command add feature file.</summary>
    public void MonitorCommandAddFeatureFile(ProjectSettings projectSettings) { }
    /// <summary>Gets or sets the monitor command add reqnroll config file.</summary>
    public void MonitorCommandAddReqnrollConfigFile(ProjectSettings projectSettings) { }
    /// <summary>Gets or sets the monitor error.</summary>
    public void MonitorError(System.Exception exception, bool? isFatal = null) { }
    /// <summary>Gets or sets the monitor project template wizard started.</summary>
    public void MonitorProjectTemplateWizardStarted() { }
    /// <summary>Gets or sets the monitor project template wizard completed.</summary>
    public void MonitorProjectTemplateWizardCompleted(string dotNetFramework, string unitTestFramework, bool addFluentAssertions) { }
    /// <summary>Gets or sets the monitor upgrade dialog dismissed.</summary>
    public void MonitorUpgradeDialogDismissed(Dictionary<string, object> additionalProps) { }
    /// <summary>Gets or sets the monitor welcome dialog dismissed.</summary>
    public void MonitorWelcomeDialogDismissed(Dictionary<string, object> additionalProps) { }
    /// <summary>Gets or sets the monitor link clicked.</summary>
    public void MonitorLinkClicked(string source, string url, Dictionary<string, object> additionalProps = null) { }
    /// <summary>Gets or sets the transmit event.</summary>
    public void TransmitEvent(ITelemetryEvent runtimeEvent) { }
}
