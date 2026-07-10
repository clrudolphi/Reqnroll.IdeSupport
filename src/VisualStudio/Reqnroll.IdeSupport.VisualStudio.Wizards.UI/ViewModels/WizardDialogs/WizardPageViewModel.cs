// Ported from Reqnroll.VisualStudio\UI\ViewModels\WizardDialogs\WizardPageViewModel.cs
namespace Reqnroll.IdeSupport.VisualStudio.Wizards.UI.ViewModels.WizardDialogs;

/// <summary>Base view model for a single page shown within a <see cref="WizardViewModel"/>.</summary>
public class WizardPageViewModel : INotifyPropertyChanged
{
    private bool _isActive;

    /// <summary>Creates the page with the given display name.</summary>
    public WizardPageViewModel(string name)
    {
        Name = name;
    }

    /// <summary>The page's display name (shown e.g. as a tab label).</summary>
    public string Name { get; }

    /// <summary>Whether this page is the currently displayed page in the wizard.</summary>
    public bool IsActive
    {
        get => _isActive;
        set
        {
            if (value == _isActive) return;
            _isActive = value;
            OnPropertyChanged();
        }
    }

    /// <inheritdoc/>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>Raises <see cref="PropertyChanged"/> for the given (or calling) property name.</summary>
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
