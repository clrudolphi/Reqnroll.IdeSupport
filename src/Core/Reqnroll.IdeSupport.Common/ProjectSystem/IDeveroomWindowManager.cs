using System;
using System.Linq;

namespace Reqnroll.IdeSupport.VisualStudio.ProjectSystem;

/// <summary>IDeveroomWindowManager</summary>
public interface IDeveroomWindowManager
{
    /// <summary>Shows a modal dialog for the specified view-model.</summary>
    bool? ShowDialog<TViewModel>(TViewModel viewModel);
}
