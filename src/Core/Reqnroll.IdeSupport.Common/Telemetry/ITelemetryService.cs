using System;
using System.Collections.Generic;
using Reqnroll.IdeSupport.Common.ProjectSystem.Settings;

namespace Reqnroll.IdeSupport.Common.Telemetry;

/// <summary>ITelemetryService</summary>
public interface ITelemetryService
{
    /// <summary>Gets or sets the monitor load project system.</summary>
    void MonitorLoadProjectSystem();
    /// <summary>Gets or sets the monitor open project system.</summary>
    void MonitorOpenProjectSystem(IIdeScope ideScope);
    /// <summary>Gets or sets the monitor open project.</summary>
    void MonitorOpenProject(ProjectSettings settings, int? featureFileCount);
    /// <summary>Gets or sets the monitor open feature file.</summary>
    void MonitorOpenFeatureFile(ProjectSettings projectSettings);
    //void MonitorParserParse(ProjectSettings settings, Dictionary<string, object> additionalProps);

    /// <summary>Gets or sets the monitor extension installed.</summary>
    void MonitorExtensionInstalled();
    /// <summary>Gets or sets the monitor extension upgraded.</summary>
    void MonitorExtensionUpgraded(string oldExtensionVersion);
    /// <summary>Gets or sets the monitor extension days of usage.</summary>
    void MonitorExtensionDaysOfUsage(int usageDays);

    //void MonitorCommandCommentUncomment();
    //void MonitorCommandDefineSteps(CreateStepDefinitionsDialogResult action, int snippetCount);
    //void MonitorCommandFindStepDefinitionUsages(int usagesCount, bool isCancelled);
    //void MonitorCommandFindUnusedStepDefinitions(int unusedStepDefinitions, int scannedFeatureFiles, bool isCancellationRequested);
    //void MonitorCommandGoToStepDefinition(bool generateSnippet);
    //void MonitorCommandGoToHook();
    //void MonitorCommandAutoFormatTable();
    //void MonitorCommandAutoFormatDocument(bool isSelectionFormatting);
    /// <summary>Gets or sets the monitor command add feature file.</summary>
    void MonitorCommandAddFeatureFile(ProjectSettings projectSettings);
    /// <summary>Gets or sets the monitor command add reqnroll config file.</summary>
    void MonitorCommandAddReqnrollConfigFile(ProjectSettings projectSettings);
    //void MonitorCommandRenameStepExecuted(RenameStepCommandContext ctx);

    //void MonitorReqnrollGeneration(bool isFailed, ProjectSettings projectSettings);

    /// <summary>Gets or sets the monitor error.</summary>
    void MonitorError(Exception exception, bool? isFatal = null);

    /// <summary>Gets or sets the monitor project template wizard started.</summary>
    void MonitorProjectTemplateWizardStarted();

    /// <summary>Gets or sets the monitor project template wizard completed.</summary>
    void MonitorProjectTemplateWizardCompleted(string dotNetFramework, string unitTestFramework,
        bool addFluentAssertions);

    //void MonitorNotificationShown(NotificationData notification);
    //void MonitorNotificationDismissed(NotificationData notification);
    /// <summary>Gets or sets the monitor link clicked.</summary>
    void MonitorLinkClicked(string source, string url, Dictionary<string, object> additionalProps = null);

    /// <summary>Gets or sets the monitor upgrade dialog dismissed.</summary>
    void MonitorUpgradeDialogDismissed(Dictionary<string, object> additionalProps);
    /// <summary>Gets or sets the monitor welcome dialog dismissed.</summary>
    void MonitorWelcomeDialogDismissed(Dictionary<string, object> additionalProps);
    /// <summary>Gets or sets the transmit event.</summary>
    void TransmitEvent(ITelemetryEvent runtimeEvent);
}
