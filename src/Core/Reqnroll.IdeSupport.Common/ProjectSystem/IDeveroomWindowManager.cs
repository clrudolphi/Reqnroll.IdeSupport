using System;
using System.Linq;

namespace Reqnroll.IdeSupport.VisualStudio.ProjectSystem;

/// <summary>IDeveroomWindowManager</summary>
public interface IDeveroomWindowManager
{
    /// <summary>Gets or sets the show dialog<tview model>.</summary>
    bool? ShowDialog<TViewModel>(TViewModel viewModel);
}
