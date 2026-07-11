#nullable enable

namespace Reqnroll.IdeSupport.VisualStudio.Extension.RenameStep;

/// <summary>
/// Shared, container-registered holder for the runtime-created Step Rename refactoring components.
/// Follows the same pattern as <see cref="FindStepUsages.FindStepUsagesState"/>.
/// </summary>
internal sealed class RenameStepState
{
    /// <summary>Set once the server has initialised; null before that and after dispose.</summary>
    public RenameStepService? Service { get; set; }
}
