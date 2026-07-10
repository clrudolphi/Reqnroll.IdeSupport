// Ported from Reqnroll.VisualStudio.UI\Dialogs\AddNewReqnrollProjectDialog.xaml.cs
// IVsUIShell dependency removed.
using System.Windows.Controls;
using Reqnroll.IdeSupport.VisualStudio.Wizards.UI.Dialogs;
using Reqnroll.IdeSupport.VisualStudio.Wizards.UI.ViewModels;

namespace Reqnroll.IdeSupport.VisualStudio.Wizards.UI.Dialogs;

/// <summary>Dialog for configuring a new Reqnroll project (target framework and unit test framework).</summary>
public partial class AddNewReqnrollProjectDialog : WizardWindow
{
    /// <summary>Creates the dialog without a view model (used by the WPF designer).</summary>
    public AddNewReqnrollProjectDialog()
    {
        InitializeComponent();
    }

    /// <summary>Creates the dialog bound to the given view model.</summary>
    public AddNewReqnrollProjectDialog(AddNewReqnrollProjectViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
    }

    /// <summary>The view model backing this dialog.</summary>
    public AddNewReqnrollProjectViewModel? ViewModel { get; }

    private void CreateButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void TestFramework_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.Count == 0) return;
        if (ViewModel != null)
            ViewModel.UnitTestFramework = e.AddedItems[0]?.ToString() ?? string.Empty;
        e.Handled = true;
    }
}
