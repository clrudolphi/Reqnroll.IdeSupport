// VsIntegration layer — VS SDK references are expected here.
// This file implements IWizardContext by adapting the VS DTE / IVsIdeScope
// types that are available in the original Reqnroll.VisualStudio assembly.
//
// NOTE: The using aliases below reference types from the original solution's
// assemblies. When this project is moved to the new LSP extension, replace
// with whatever the new project system provides.

using Reqnroll.IdeSupport.Common.ProjectSystem.Settings;
using Reqnroll.IdeSupport.VisualStudio.Wizards.Abstractions;



// Aliases to types from Reqnroll.VisualStudio — available via project reference
// when compiled inside the original solution.
using OriginalIdeScope = Reqnroll.IdeSupport.Common.IIdeScope;
using OriginalProjectScope = Reqnroll.IdeSupport.Common.ProjectSystem.IProjectScope;
using OriginalProjectSettings = Reqnroll.IdeSupport.Common.ProjectSystem.Settings.ProjectSettings;

namespace Reqnroll.IdeSupport.VisualStudio.Wizards.VsIntegration;

/// <summary>
/// Implements IWizardContext using the full VS DTE / IProjectScope objects
/// that are available inside the original Reqnroll.VisualStudio extension.
/// </summary>
public class VsWizardContext : IWizardContext
{
    private readonly OriginalProjectScope? _projectScope;
    private readonly OriginalIdeScope _ideScope;

    /// <summary>
    /// Builds the context, mapping the original project's settings into
    /// <see cref="WizardProjectSettings"/> when a project scope is available.
    /// </summary>
    public VsWizardContext(
        bool isAddNewItem,
        OriginalProjectScope? projectScope,
        OriginalIdeScope ideScope,
        string templateFolder,
        string targetFolder,
        string targetFileName,
        Dictionary<string, string> replacementsDictionary,
        IWizardDialogService dialogService,
        IWizardTelemetry telemetry)
    {
        IsAddNewItem = isAddNewItem;
        _projectScope = projectScope;
        _ideScope = ideScope;
        TemplateFolder = templateFolder;
        TargetFolder = targetFolder;
        TargetFileName = targetFileName;
        ReplacementsDictionary = replacementsDictionary;
        DialogService = dialogService;
        Telemetry = telemetry;

        ProjectSettings = projectScope != null
            ? MapProjectSettings(projectScope.GetProjectSettings())
            : WizardProjectSettings.Uninitialized;
    }

    /// <summary>Whether the wizard is adding a single item vs. creating a new project.</summary>
    public bool IsAddNewItem { get; }
    /// <summary>Folder containing the source template files.</summary>
    public string TemplateFolder { get; }
    /// <summary>Folder the generated item/project should be placed in.</summary>
    public string TargetFolder { get; }
    /// <summary>File name (without extension resolution) of the generated item.</summary>
    public string TargetFileName { get; set; }
    /// <summary>Template token replacements supplied by VS for this wizard run.</summary>
    public Dictionary<string, string> ReplacementsDictionary { get; }
    /// <summary>Reqnroll/SpecFlow-related settings of the target project.</summary>
    public WizardProjectSettings ProjectSettings { get; }
    /// <summary>Service used to show wizard dialogs.</summary>
    public IWizardDialogService DialogService { get; }
    /// <summary>Telemetry sink for wizard events.</summary>
    public IWizardTelemetry Telemetry { get; }

    /// <summary>Reports a problem message to the IDE's action surface (e.g. an error bar).</summary>
    public void ShowProblem(string message) =>
        _ideScope.Actions.ShowProblem(message);

    private static WizardProjectSettings MapProjectSettings(OriginalProjectSettings s) => new()
    {
        IsReqnrollProject = s.IsReqnrollProject,
        IsSpecFlowProject = s.IsSpecFlowProject,
        DesignTimeFeatureFileGenerationEnabled = s.DesignTimeFeatureFileGenerationEnabled,
        HasDesignTimeGenerationReplacement = s.HasDesignTimeGenerationReplacement,
        HasXUnitAdapter = s.ReqnrollProjectTraits.HasFlag(
            ReqnrollProjectTraits.XUnitAdapter),
        ReqnrollVersionLabel = s.GetReqnrollVersionLabel(),
        ShortLabel = s.GetShortLabel()
    };
}
