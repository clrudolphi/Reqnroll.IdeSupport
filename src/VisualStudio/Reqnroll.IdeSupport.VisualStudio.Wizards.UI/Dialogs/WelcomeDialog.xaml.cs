// Ported from Reqnroll.VisualStudio.UI\Dialogs\WelcomeDialog.xaml.cs
// IVsUIShell dependency removed — WizardWindow base handles hosting.
using Reqnroll.IdeSupport.VisualStudio.Wizards.UI.Dialogs;
using Reqnroll.IdeSupport.VisualStudio.Wizards.UI.ViewModels.WizardDialogs;

namespace Reqnroll.IdeSupport.VisualStudio.Wizards.UI.Dialogs;

/// <summary>
/// Dialog used for both the first-install welcome flow and the upgrade/changelog flow,
/// driven by whichever <see cref="WizardViewModel"/> is supplied.
/// </summary>
public partial class WelcomeDialog : WizardWindow
{
    /// <summary>Creates the dialog without a view model (used by the WPF designer).</summary>
    public WelcomeDialog()
    {
        InitializeComponent();
    }

    /// <summary>Creates the dialog bound to the given view model.</summary>
    public WelcomeDialog(WizardViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
    }

    /// <summary>The view model backing this dialog.</summary>
    public WizardViewModel? ViewModel { get; }
}
