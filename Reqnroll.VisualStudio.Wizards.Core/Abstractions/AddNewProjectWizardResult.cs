namespace Reqnroll.VisualStudio.Wizards.Abstractions;

/// <summary>
/// Data returned by IWizardDialogService.ShowAddNewProjectDialog().
/// Lives in Core/Abstractions so wizard logic can read selections
/// without any dependency on WPF ViewModels or the UI assembly.
/// </summary>
public sealed class AddNewProjectWizardResult
{
    public AddNewProjectWizardResult(
        string dotNetFramework,
        string unitTestFramework,
        bool fluentAssertionsIncluded)
    {
        DotNetFramework = dotNetFramework;
        UnitTestFramework = unitTestFramework;
        FluentAssertionsIncluded = fluentAssertionsIncluded;
    }

    public string DotNetFramework { get; }
    public string UnitTestFramework { get; }
    public bool FluentAssertionsIncluded { get; }

    public bool IsNetFramework =>
        DotNetFramework.StartsWith("net4", StringComparison.OrdinalIgnoreCase);
}
