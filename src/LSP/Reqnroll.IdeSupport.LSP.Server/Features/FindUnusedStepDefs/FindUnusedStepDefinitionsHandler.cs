using Reqnroll.IdeSupport.LSP.Core.FindUnusedStepDefs;
using Reqnroll.IdeSupport.LSP.Server.Registry;
using Reqnroll.IdeSupport.LSP.Server.Telemetry;

namespace Reqnroll.IdeSupport.LSP.Server.Features.FindUnusedStepDefs;

/// <summary>
/// Handles the custom <c>reqnroll/findUnusedStepDefinitions</c> request (F15). Resolves all
/// project binding registries and delegates the scan/dedupe/match algorithm to
/// <see cref="IFindUnusedStepDefinitionsService"/> (LSP.Core), then maps the result to the wire
/// <see cref="FindUnusedStepDefinitionsResponse"/> shape and fires telemetry.
/// </summary>
public sealed class FindUnusedStepDefinitionsHandler
{
    private readonly IProjectBindingRegistryLookup _registryLookup;
    private readonly IFindUnusedStepDefinitionsService _service;
    private readonly ILspTelemetryService? _telemetryService;

    public FindUnusedStepDefinitionsHandler(
        IProjectBindingRegistryLookup registryLookup,
        IFindUnusedStepDefinitionsService service,
        ILspTelemetryService? telemetryService = null)
    {
        _registryLookup = registryLookup;
        _service = service;
        _telemetryService = telemetryService;
    }

    public Task<FindUnusedStepDefinitionsResponse> HandleAsync(CancellationToken cancellationToken)
    {
        var allRegistries = _registryLookup.GetAllRegistries();

        var unused = _service.FindUnusedStepDefinitions(
            allRegistries.Select(r => (r.ProjectName, r.Registry)).ToList());

        var items = unused.Select(u => new UnusedStepDefinitionItem
        {
            ProjectName = u.ProjectName,
            ClassName = u.ClassName,
            MethodName = u.MethodName,
            BindingExpression = u.BindingExpression,
            SourceFile = u.SourceFile,
            SourceLine = u.SourceLine - 1,     // 1-based → 0-based
            SourceChar = u.SourceColumn - 1,   // 1-based → 0-based
        }).ToList();

        _telemetryService?.SendEvent("FindUnusedStepDefinitions command executed", new()
        {
            ["UnusedStepDefinitions"] = items.Count,
            ["ScannedFeatureFiles"] = allRegistries.Count,
            ["IsCancellationRequested"] = false,
        });

        return Task.FromResult(new FindUnusedStepDefinitionsResponse { Items = items });
    }
}
