#nullable disable

using Gherkin;
using Gherkin.Ast;
using GherkinLocation = Gherkin.Ast.Location;
using Reqnroll.IdeSupport.LSP.Core.Bindings;
using Reqnroll.IdeSupport.LSP.Core.Documents;
using Reqnroll.IdeSupport.LSP.Core.Parsing.Gherkin;
using Reqnroll.IdeSupport.LSP.Core.TagExpressions;
using System.Text.RegularExpressions;

namespace Reqnroll.IdeSupport.LSP.TestStubs;

/// <summary>
/// Shared base class for <see cref="ProjectBindingRegistry"/> tests.
/// Contains the common helper methods that do not require internal access to LSP.Core types.
/// Feature-structure building methods (CreateScenarioContext, CreateBackgroundContext, etc.)
/// remain in per-project copies due to <c>InternalsVisibleTo</c> constraints.
/// </summary>
public abstract class ProjectBindingRegistryTestsBase
{
    protected readonly List<ProjectStepDefinitionBinding> _stepDefinitionBindings = new();
    protected readonly List<ProjectHookBinding> _hookBindings = new();
    protected readonly Dictionary<string, ProjectBindingImplementation> Implementations = new();

    protected ProjectBindingRegistry CreateSut()
    {
        var projectBindingRegistry = new ProjectBindingRegistry(_stepDefinitionBindings.ToArray(), _hookBindings.ToArray(), 123456);
        return projectBindingRegistry;
    }

    protected Step CreateStep(StepKeyword stepKeyword = StepKeyword.Given, string text = "my step",
        StepArgument stepArgument = null) => new DeveroomGherkinStep(new GherkinLocation(0, 0), stepKeyword + " ", StepKeywordType.Context, text, stepArgument,
        stepKeyword, (ScenarioBlock) stepKeyword);

    protected ProjectStepDefinitionBinding CreateStepDefinitionBinding(string regex,
        ScenarioBlock scenarioBlock = ScenarioBlock.Given, BindingScope scope = null, string[] parameterTypes = null,
        string methodName = null)
    {
        methodName = methodName ?? "MyMethod" + Guid.NewGuid().ToString("N");
        if (!Implementations.TryGetValue(methodName, out var implementation))
        {
            implementation =
                new ProjectBindingImplementation(methodName, parameterTypes,
                    new SourceLocation("MyClass.cs", 2, 5));
            Implementations.Add(methodName, implementation);
        }

        return new ProjectStepDefinitionBinding(scenarioBlock, new Regex("^" + regex + "$"), scope, implementation);
    }

    protected StepArgument CreateDocString() => new DocString(new GherkinLocation(0, 0), null, "some text");

    protected static DataTable CreateDataTable()
    {
        return new DataTable(new List<TableRow>
        {
            new TableRow(new GherkinLocation(0, 0), new[] {new TableCell(new GherkinLocation(0, 0), "cell1")})
        });
    }

    protected BindingScope CreateTagScope(string tagName) => new() {Tag = ReqnrollTagExpressionParser.CreateTagLiteral(tagName)};

    protected string[] GetParameterTypes(params string[] typeNames)
    {
        if (typeNames == null || typeNames.Length == 0)
            return null;

        return typeNames.Select(GetParameterType).ToArray();
    }

    protected string GetParameterType(string typeName)
    {
        switch (typeName)
        {
            case "string":
                return typeof(string).FullName;
            case "int":
                return typeof(int).FullName;
            case "DataTable":
                return "Reqnroll.Table";
            default:
                return typeName;
        }
    }
}
