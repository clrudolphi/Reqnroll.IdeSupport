#nullable enable

using Reqnroll.IdeSupport.LSP.Core.Bindings;

namespace Reqnroll.IdeSupport.LSP.Core.FindUnusedStepDefs;

public interface IFindUnusedStepDefinitionsService
{
    /// <summary>
    /// Scans every supplied project's binding registry and returns one <see cref="UnusedStepDefinition"/>
    /// per step-definition binding expression with zero matching steps across the workspace.
    /// </summary>
    IReadOnlyList<UnusedStepDefinition> FindUnusedStepDefinitions(
        IReadOnlyList<(string ProjectName, ProjectBindingRegistry Registry)> registries);
}
