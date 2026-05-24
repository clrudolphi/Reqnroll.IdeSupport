// VsIntegration layer — VS SDK references are expected here.
using Microsoft.Internal.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Reqnroll.VisualStudio.Wizards.UI;
using Reqnroll.VisualStudio.Wizards.UI.Dialogs;
using Reqnroll.VisualStudio.Wizards.UI.ViewModels;
using Reqnroll.VisualStudio.Wizards.UI.ViewModels.WizardDialogs;

namespace Reqnroll.VisualStudio.Wizards.VsIntegration;

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
        ApplyTheme(dialog);
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
        ApplyTheme(dialog);
        WindowHelper.ShowModal(dialog);
    }

    //public void ShowUpgradeDialog(string newVersion, string changeLog)
    //{
    //    var vm = new UpgradeDialogViewModel(newVersion, changeLog);
    //    var dialog = new UpgradeDialog(vm);
    //    ApplyTheme(dialog);
    //    WindowHelper.ShowModal(dialog);
    //}

    private static void ApplyTheme(System.Windows.Window dialog)
    {
        var wizardResources = dialog.Resources.MergedDictionaries
            .OfType<WizardResources>()
            .FirstOrDefault();
        wizardResources?.ApplyVsTheme(VsThemeResourceProvider.GetThemedResources());
    }
}
