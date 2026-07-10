// Ported from Reqnroll.VisualStudio\UI\ViewModels\AddNewReqnrollProjectViewModel.cs
#nullable disable

namespace Reqnroll.IdeSupport.VisualStudio.Wizards.UI.ViewModels;

/// <summary>View model backing the "add new Reqnroll project" dialog.</summary>
public class AddNewReqnrollProjectViewModel : INotifyPropertyChanged
{
    private const string MsTest = "MsTest";
    private const string Net8 = "net8.0";
    private const string Net9 = "net9.0";
    private const string Net10 = "net10.0";

#if DEBUG
    /// <summary>Sample data used by the WPF designer.</summary>
    public static AddNewReqnrollProjectViewModel DesignData = new()
    {
        DotNetFramework = Net9,
        UnitTestFramework = MsTest,
    };
#endif

    private string _dotNetFramework = Net8;

    /// <summary>The target .NET framework moniker selected by the user (e.g. "net8.0").</summary>
    public string DotNetFramework
    {
        get => _dotNetFramework;
        set
        {
            _dotNetFramework = value;
            OnPropertyChanged(nameof(TestFrameworks));
        }
    }

    /// <summary>Whether the selected <see cref="DotNetFramework"/> is a .NET Framework (net4x) target.</summary>
    public bool IsNetFramework =>
        DotNetFramework.StartsWith("net4", StringComparison.InvariantCultureIgnoreCase);

    /// <summary>The unit test framework selected by the user.</summary>
    public string UnitTestFramework { get; set; } = MsTest;


    /// <summary>The list of unit test frameworks offered to the user.</summary>
    public ObservableCollection<string> TestFrameworks { get; } =
        new(new List<string> { "MSTest", "NUnit", "xUnit", "xUnit.v3", "TUnit" });

    /// <inheritdoc/>
    public event PropertyChangedEventHandler PropertyChanged;

    /// <summary>Raises <see cref="PropertyChanged"/> for the given (or calling) property name.</summary>
    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
