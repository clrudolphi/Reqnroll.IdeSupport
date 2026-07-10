#nullable disable
using Reqnroll.IdeSupport.Common;
using Reqnroll.IdeSupport.Common.Telemetry;
using Reqnroll.IdeSupport.Common.ProjectSystem.Settings;
using System.Collections.Immutable;
using System.ComponentModel.Composition;

namespace Reqnroll.IdeSupport.VisualStudio.Telemetry;

[Export(typeof(ITelemetryService))]
public class TelemetryService : ITelemetryService
{
    private readonly ITelemetryTransmitter _telemetryTransmitter;
    //private readonly IWelcomeService _welcomeService;

    [ImportingConstructor]
    public TelemetryService(ITelemetryTransmitter telemetryTransmitter)
    {
        _telemetryTransmitter = telemetryTransmitter;
    }

    // OPEN

    public void MonitorLoadProjectSystem()
    {
        //currently we do nothing at this point
    }

    public void MonitorOpenProjectSystem(IIdeScope ideScope)
    {
        //_welcomeService.OnIdeScopeActivityStarted(ideScope, this);

        _telemetryTransmitter.TransmitEvent(new GenericEvent("Extension loaded"));
    }

    public void MonitorOpenProject(ProjectSettings settings, int? featureFileCount)
    {
        _telemetryTransmitter.TransmitEvent(new GenericEvent("Project loaded",
            GetProjectSettingsProps(settings,
                new Dictionary<string, object>
                {
                    {"FeatureFileCount", featureFileCount}
                }
            )));
    }

    public void MonitorOpenFeatureFile(ProjectSettings projectSettings)
    {
        _telemetryTransmitter.TransmitEvent(new GenericEvent("Feature file opened",
            GetProjectSettingsProps(projectSettings)));
    }

    public void MonitorParserParse(ProjectSettings settings, Dictionary<string, object> additionalProps)
    {
        _telemetryTransmitter.TransmitEvent(new GenericEvent("Feature file parsed",
            GetProjectSettingsProps(settings, additionalProps)));
    }


    // EXTENSION

    public void MonitorExtensionInstalled()
    {
        _telemetryTransmitter.TransmitEvent(new GenericEvent("Extension installed"));
    }

    public void MonitorExtensionUpgraded(string oldExtensionVersion)
    {
        _telemetryTransmitter.TransmitEvent(new GenericEvent("Extension upgraded",
            new Dictionary<string, object>
            {
                {"OldExtensionVersion", oldExtensionVersion}
            }));
    }

    public void MonitorExtensionDaysOfUsage(int usageDays)
    {
        _telemetryTransmitter.TransmitEvent(new GenericEvent($"{usageDays} day usage"));
    }


    //COMMAND

    public void MonitorCommandAddFeatureFile(ProjectSettings settings)
    {
        _telemetryTransmitter.TransmitEvent(new GenericEvent("Feature file added",
            GetProjectSettingsProps(settings)));
    }

    public void MonitorCommandAddReqnrollConfigFile(ProjectSettings settings)
    {
        _telemetryTransmitter.TransmitEvent(new GenericEvent("Reqnroll config added",
            GetProjectSettingsProps(settings)));
    }

    //REQNROLL

    public void MonitorReqnrollGeneration(bool isFailed, ProjectSettings projectSettings)
    {
        _telemetryTransmitter.TransmitEvent(new GenericEvent("Reqnroll Generation executed",
            GetProjectSettingsProps(projectSettings,
                new Dictionary<string, object>
                {
                    {"IsFailed", isFailed}
                })));
    }

    //ERROR

    public void MonitorError(Exception exception, bool? isFatal = null)
    {
        if (isFatal.HasValue)
            _telemetryTransmitter.TransmitFatalExceptionEvent(exception, isFatal.Value);
        else
            _telemetryTransmitter.TransmitExceptionEvent(exception, ImmutableDictionary<string, object>.Empty);
    }


    // PROJECT TEMPLATE WIZARD

    public void MonitorProjectTemplateWizardStarted()
    {
        _telemetryTransmitter.TransmitEvent(new GenericEvent("Project Template Wizard Started"));
    }

    public void MonitorProjectTemplateWizardCompleted(string dotNetFramework, string unitTestFramework,
        bool addFluentAssertions)
    {
        _telemetryTransmitter.TransmitEvent(new GenericEvent("Project Template Wizard Completed",
            new Dictionary<string, object>
            {
                {"SelectedDotNetFramework", dotNetFramework},
                {"SelectedUnitTestFramework", unitTestFramework},
                {"AddFluentAssertions", addFluentAssertions}
            }));
    }


    //public void MonitorNotificationShown(NotificationData notification)
    //{
    //    _telemetryTransmitter.TransmitEvent(new GenericEvent("Notification shown",
    //        GetNotificationProps(notification)));
    //}

    //public void MonitorNotificationDismissed(NotificationData notification)
    //{
    //    _telemetryTransmitter.TransmitEvent(new GenericEvent("Notification dismissed",
    //        GetNotificationProps(notification)));
    //}

    public void MonitorLinkClicked(string source, string url, Dictionary<string, object> additionalProps = null)
    {
        additionalProps ??= new Dictionary<string, object>();
        additionalProps.Add("Source", source);
        additionalProps.Add("URL", url);
        _telemetryTransmitter.TransmitEvent(new GenericEvent("Link clicked",
            additionalProps));
    }

    public void MonitorUpgradeDialogDismissed(Dictionary<string, object> additionalProps)
    {
        _telemetryTransmitter.TransmitEvent(new GenericEvent("Upgrade dialog dismissed",
            additionalProps));
    }

    public void MonitorWelcomeDialogDismissed(Dictionary<string, object> additionalProps)
    {
        _telemetryTransmitter.TransmitEvent(new GenericEvent("Welcome dialog dismissed",
            additionalProps));
    }

    public void TransmitEvent(ITelemetryEvent runtimeEvent)
        => _telemetryTransmitter.TransmitEvent(runtimeEvent);


    private ImmutableDictionary<string, object> GetProjectSettingsProps(ProjectSettings settings)
    {
        var props = GetProps(settings);
        return props.ToImmutable();
    }

    private ImmutableDictionary<string, object> GetProjectSettingsProps(ProjectSettings settings,
        IEnumerable<KeyValuePair<string, object>> additionalSettings)
    {
        var props = GetProps(settings);
        props.AddRange(additionalSettings);
        return props.ToImmutable();
    }

    private static ImmutableDictionary<string, object>.Builder GetProps(ProjectSettings settings)
    {
        var props = ImmutableDictionary.CreateBuilder<string, object>();

        if (settings == null) return props;

        props.Add("ReqnrollVersion", settings.GetReqnrollVersionLabel());
        props.Add("ProjectTargetFramework", settings.TargetFrameworkMonikers);
        props.Add("SingleFileGeneratorUsed", settings.DesignTimeFeatureFileGenerationEnabled);
        props.Add("ProgrammingLanguage", settings.ProgrammingLanguage);
        if (settings.IsSpecFlowProject)
            props.Add("LegacySpecFlow", true);
        return props;
    }
}
