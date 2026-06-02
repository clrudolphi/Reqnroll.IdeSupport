using Reqnroll;
using Reqnroll.IdeSupport.LSP.Server.Specs.Support;

namespace Reqnroll.IdeSupport.LSP.Server.Specs.Hooks;

[Binding]
public sealed class ScenarioHooks
{
    private readonly LspScenarioContext _context;

    public ScenarioHooks(LspScenarioContext context) => _context = context;

    [AfterScenario]
    public async Task TearDownAsync() => await _context.DisposeAsync();
}
