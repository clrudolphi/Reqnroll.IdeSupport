// VsIntegration layer — VS SDK references are expected here.
using Microsoft.Internal.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Reqnroll.VisualStudio.Wizards.UI.Dialogs;
using Reqnroll.VisualStudio.Wizards.UI.ViewModels;
using Reqnroll.VisualStudio.Wizards.UI.ViewModels.WizardDialogs;

namespace Reqnroll.VisualStudio.Wizards.VsIntegration;

/// <summary>
/// Implements IWizardDialogService using VS modal hosting (IVsUIShell / WindowHelper).
/// This is the only place in the wizard stack that touches WPF ViewModel types
/// and the VS UI shell — Core wizard logic is insulated via AddNewProjectWizardResult.
/// </summary>
public class VsWizardDialogService : IWizardDialogService
{
    private readonly IVsUIShell _vsUiShell;

    public VsWizardDialogService(IVsUIShell vsUiShell)
    {
        _vsUiShell = vsUiShell;
    }

    public AddNewProjectWizardResult? ShowAddNewProjectDialog()
    {
        var vm = new AddNewReqnrollProjectViewModel();
        var dialog = new AddNewReqnrollProjectDialog(vm);
        int result = WindowHelper.ShowModal(dialog);
        if (result != 1)
            return null;

        return new AddNewProjectWizardResult(
            vm.DotNetFramework,
            vm.UnitTestFramework,
            vm.FluentAssertionsIncluded);
    }

    public void ShowWelcomeDialog()
    {
        var vm = new WelcomeDialogViewModel();
        var dialog = new WelcomeDialog(vm);
        WindowHelper.ShowModal(dialog);
    }

    public void ShowUpgradeDialog(string newVersion, string changeLog)
    {
        var vm = new UpgradeDialogViewModel(newVersion, changeLog);
        var dialog = new WelcomeDialog(vm);
        WindowHelper.ShowModal(dialog);
    }
}
