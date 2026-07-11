// Ported from Reqnroll.VisualStudio\UI\ViewModels\WizardDialogs\WizardViewModel.cs
// DelegateCommand (VS SDK) replaced with RelayCommand (portable).

namespace Reqnroll.IdeSupport.VisualStudio.Wizards.UI.ViewModels.WizardDialogs;

/// <summary>
/// Drives a multi-page wizard dialog: tracks the active page, visited pages, and the
/// next/previous navigation commands.
/// </summary>
public class WizardViewModel : INotifyPropertyChanged
{
    /// <summary>Creates the wizard with the given pages, activating the first one.</summary>
    public WizardViewModel(string finishButtonLabel, string dialogTitle, params WizardPageViewModel[] pages)
    {
        VisitedPages = new HashSet<WizardPageViewModel>();
        FinishButtonLabel = finishButtonLabel;
        DialogTitle = dialogTitle;
        NextCommand = new RelayCommand(_ => MovePageBy(1), _ => CanMovePageBy(1));
        PreviousCommand = new RelayCommand(_ => MovePageBy(-1), _ => CanMovePageBy(-1));
        Pages = new ObservableCollection<WizardPageViewModel>();
        if (pages != null)
            foreach (var page in pages)
                Pages.Add(page);
        if (Pages.Count > 0)
            MoveToPage(0);
    }

    /// <summary>The dialog's window title.</summary>
    public string DialogTitle { get; }
    /// <summary>Label shown on the finish/close button.</summary>
    public string FinishButtonLabel { get; }
    /// <summary>The wizard's pages, in navigation order.</summary>
    public ObservableCollection<WizardPageViewModel> Pages { get; }
    /// <summary>The set of pages the user has navigated to during this wizard run.</summary>
    public HashSet<WizardPageViewModel> VisitedPages { get; }

    /// <summary>Command that navigates to the previous page.</summary>
    public ICommand PreviousCommand { get; }
    /// <summary>Command that navigates to the next page.</summary>
    public ICommand NextCommand { get; }

    /// <summary>The currently active page, or <c>null</c> if none is active.</summary>
    public WizardPageViewModel? ActivePage => Pages.FirstOrDefault(p => p.IsActive);

    /// <summary>The index of <see cref="ActivePage"/> within <see cref="Pages"/> (0 if none is active).</summary>
    public int ActivePageIndex
    {
        get
        {
            var activePage = ActivePage;
            return activePage == null ? 0 : Pages.IndexOf(activePage);
        }
    }

    /// <summary>Whether the active page is the last page in the wizard.</summary>
    public bool IsOnLastPage => ActivePageIndex == Pages.Count - 1;

    /// <inheritdoc/>
    public event PropertyChangedEventHandler? PropertyChanged;

    private bool CanMovePageBy(int step)
    {
        int newPageIndex = ActivePageIndex + step;
        return newPageIndex >= 0 && newPageIndex < Pages.Count;
    }

    private void MovePageBy(int step)
    {
        MoveToPage(ActivePageIndex + step, ActivePageIndex);
    }

    private void MoveToPage(int newPageIndex, int activePageIndex = -1)
    {
        if (activePageIndex < 0)
            activePageIndex = ActivePageIndex;
        if (newPageIndex < 0 || newPageIndex >= Pages.Count)
            return;
        Pages[activePageIndex].IsActive = false;
        var newPage = Pages[newPageIndex];
        newPage.IsActive = true;
        VisitedPages.Add(newPage);
        OnPropertyChanged(nameof(ActivePage));
        OnPropertyChanged(nameof(ActivePageIndex));
        OnPropertyChanged(nameof(IsOnLastPage));
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
