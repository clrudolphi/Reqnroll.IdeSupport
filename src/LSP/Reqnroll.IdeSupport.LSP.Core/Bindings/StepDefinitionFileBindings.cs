namespace Reqnroll.IdeSupport.LSP.Core.Bindings;

/// <summary>
/// The complete set of bindings discovered in a single C# source file by Roslyn-based
/// (source-level) discovery: step definitions and hooks.
/// </summary>
public record StepDefinitionFileBindings(
    IReadOnlyList<ProjectStepDefinitionBinding> StepDefinitions,
    IReadOnlyList<ProjectHookBinding> Hooks)
{
    /// <summary>Sentinel for a file that contains no step definitions or hooks.</summary>
    public static readonly StepDefinitionFileBindings Empty =
        new(Array.Empty<ProjectStepDefinitionBinding>(), Array.Empty<ProjectHookBinding>());
}
