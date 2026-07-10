// Ported from Reqnroll.VisualStudio\UI\ViewModels\WizardDialogs\MarkDownWizardPageViewModel.cs
namespace Reqnroll.IdeSupport.VisualStudio.Wizards.UI.ViewModels.WizardDialogs;

/// <summary>A wizard page whose content is rendered from Markdown text.</summary>
public class MarkDownWizardPageViewModel : WizardPageViewModel
{
    /// <summary>Creates the page with the given display name.</summary>
    public MarkDownWizardPageViewModel(string name) : base(name)
    {
    }

    /// <summary>The Markdown-formatted content of the page.</summary>
    public string Text { get; set; } = string.Empty;
}
