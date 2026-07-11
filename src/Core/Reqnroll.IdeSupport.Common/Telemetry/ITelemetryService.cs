using System;
using System.Collections.Generic;
using Reqnroll.IdeSupport.Common.ProjectSystem.Settings;

namespace Reqnroll.IdeSupport.Common.Telemetry;

/// <summary>ITelemetryService</summary>
public interface ITelemetryService
{
    /// <summary>Records that the project system has finished loading.</summary>
    void MonitorLoadProjectSystem();
    /// <summary>Records that a project system was opened.</summary>
    void MonitorOpenProjectSystem(IIdeScope ideScope);
    /// <summary>Records that a project was opened, including its feature file count.</summary>
    void MonitorOpenProject(ProjectSettings settings, int? featureFileCount);
    /// <summary>Records that a feature file was opened.</summary>
    void MonitorOpenFeatureFile(ProjectSettings projectSettings);
    //void MonitorParserParse(ProjectSettings settings, Dictionary<string, object> additionalProps);

    /// <summary>Records that the extension was freshly installed.</summary>
    void MonitorExtensionInstalled();
    /// <summary>Records that the extension was upgraded from <paramref name="oldExtensionVersion"/>.</summary>
    void MonitorExtensionUpgraded(string oldExtensionVersion);
    /// <summary>Records the number of days the extension has been in use.</summary>
    void MonitorExtensionDaysOfUsage(int usageDays);

    //void MonitorCommandCommentUncomment();
    //void MonitorCommandDefineSteps(CreateStepDefinitionsDialogResult action, int snippetCount);
    //void MonitorCommandFindStepDefinitionUsages(int usagesCount, bool isCancelled);
    //void MonitorCommandFindUnusedStepDefinitions(int unusedStepDefinitions, int scannedFeatureFiles, bool isCancellationRequested);
    //void MonitorCommandGoToStepDefinition(bool generateSnippet);
    //void MonitorCommandGoToHook();
    //void MonitorCommandAutoFormatTable();
    //void MonitorCommandAutoFormatDocument(bool isSelectionFormatting);
    /// <summary>Records that a feature file was added via the command.</summary>
    void MonitorCommandAddFeatureFile(ProjectSettings projectSettings);
    /// <summary>Records that a Reqnroll config file was added via the command.</summary>
    void MonitorCommandAddReqnrollConfigFile(ProjectSettings projectSettings);
    //void MonitorCommandRenameStepExecuted(RenameStepCommandContext ctx);

    //void MonitorReqnrollGeneration(bool isFailed, ProjectSettings projectSettings);

    /// <summary>Records an exception, optionally flagging whether it was fatal.</summary>
    void MonitorError(Exception exception, bool? isFatal = null);

    /// <summary>Records that the "add new Reqnroll project" wizard was started.</summary>
    void MonitorProjectTemplateWizardStarted();

    /// <summary>Records that the "add new Reqnroll project" wizard completed with the given choices.</summary>
    void MonitorProjectTemplateWizardCompleted(string dotNetFramework, string unitTestFramework,
        bool addFluentAssertions);

    //void MonitorNotificationShown(NotificationData notification);
    //void MonitorNotificationDismissed(NotificationData notification);
    /// <summary>Records that a link was clicked from the given source.</summary>
    void MonitorLinkClicked(string source, string url, Dictionary<string, object> additionalProps = null);

    /// <summary>Records that the upgrade dialog was dismissed.</summary>
    void MonitorUpgradeDialogDismissed(Dictionary<string, object> additionalProps);
    /// <summary>Records that the welcome dialog was dismissed.</summary>
    void MonitorWelcomeDialogDismissed(Dictionary<string, object> additionalProps);
    /// <summary>Transmits a fully-formed telemetry event.</summary>
    void TransmitEvent(ITelemetryEvent runtimeEvent);
}
