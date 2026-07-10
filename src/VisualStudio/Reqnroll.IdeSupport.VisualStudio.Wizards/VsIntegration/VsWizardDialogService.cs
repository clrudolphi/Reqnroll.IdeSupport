// VsIntegration layer — VS SDK references are expected here.
using Microsoft.Internal.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell.Interop;
using Reqnroll.IdeSupport.Common.Telemetry;
using Reqnroll.IdeSupport.VisualStudio.Wizards.Abstractions;

namespace Reqnroll.IdeSupport.VisualStudio.Wizards.VsIntegration;

/// <summary>
/// Shows the wizard WPF dialogs (add-new-project, welcome, upgrade), applying the
/// current VS theme and wiring telemetry for link clicks.
/// </summary>
public class VsWizardDialogService : IWizardDialogService
{
    private readonly IVsUIShell _vsUiShell;
    private readonly ITelemetryService? _telemetryService;

    /// <summary>Creates the service, optionally wiring up telemetry for dialog link clicks.</summary>
    public VsWizardDialogService(IVsUIShell vsUiShell, ITelemetryService? telemetryService = null)
    {
        _vsUiShell = vsUiShell;
        _telemetryService = telemetryService;
    }

    /// <summary>
    /// Shows the "add new Reqnroll project" dialog modally and returns the selected
    /// options, or <c>null</c> if the user cancelled.
    /// </summary>
    public AddNewProjectWizardResult? ShowAddNewProjectDialog()
    {
        var vm = new AddNewReqnrollProjectViewModel();
        var dialog = new AddNewReqnrollProjectDialog(vm);
        WireLinkClicked(dialog);
        ApplyTheme(dialog);
        int result = WindowHelper.ShowModal(dialog);
        if (result != 1)
            return null;

        return new AddNewProjectWizardResult(
            vm.DotNetFramework,
            vm.UnitTestFramework);
    }

    /// <summary>Shows the first-install welcome dialog modally.</summary>
    public void ShowWelcomeDialog()
    {
        var vm = new WelcomeDialogViewModel();
        var dialog = new WelcomeDialog(vm);
        WireLinkClicked(dialog);
        ApplyTheme(dialog);
        WindowHelper.ShowModal(dialog);
    }

    /// <summary>Shows the upgrade/changelog dialog modally for the given new version.</summary>
    public void ShowUpgradeDialog(string newVersion, string changeLog)
    {
        var vm = new UpgradeDialogViewModel(newVersion, changeLog);
        var dialog = new WelcomeDialog(vm);
        WireLinkClicked(dialog);
        ApplyTheme(dialog);
        WindowHelper.ShowModal(dialog);
    }

    private void WireLinkClicked(System.Windows.Window dialog)
    {
        if (_telemetryService is null) return;
        if (dialog is WizardWindow wizard)
        {
            wizard.LinkClicked += (sender, e) =>
            {
                var uri = e.Uri;
                if (uri is null) return;
                var uriString = uri.ToString();
                if (uriString.StartsWith("file"))
                    return;

                var source = dialog.DataContext?.GetType().Name
                                 ?.Replace("ViewModel", "") ?? "Unknown";
                _telemetryService.MonitorLinkClicked(source, uriString);
            };
        }
    }

    private static void ApplyTheme(System.Windows.Window dialog)
    {
        var wizardResources = dialog.Resources.MergedDictionaries
            .OfType<WizardResources>()
            .FirstOrDefault();
        wizardResources?.ApplyVsTheme(VsThemeResourceProvider.GetThemedResources());
    }
}
