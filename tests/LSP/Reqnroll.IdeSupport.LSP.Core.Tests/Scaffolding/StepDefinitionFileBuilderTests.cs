#nullable enable

using ApprovalTests;
using ApprovalTests.Namers;
using ApprovalTests.Reporters;
using Reqnroll.IdeSupport.Common.Configuration;
using Reqnroll.IdeSupport.LSP.Core.Scaffolding;
using Xunit;

namespace Reqnroll.IdeSupport.LSP.Core.Tests.Scaffolding;

[UseReporter /*(typeof(VisualStudioReporter))*/]
[UseApprovalSubdirectory("../ApprovalTestData")]
public class StepDefinitionFileBuilderTests
{
    // The snippet is pre-indented at one level (matching what StepSkeletonRenderer.Render produces)
    private const string Snippet = """
            [When(@"I press add")]
            public void WhenIPressAdd()
            {
                throw new PendingStepException();
            }

        """;

    [Theory]
    [InlineData("block_scoped")]
    [InlineData("file_scoped")]
    public void GenerateStepDefinitionClass(string namespaceStyle)
    {
        NamerFactory.AdditionalInformation = namespaceStyle;

        var csharpConfig = new CSharpCodeGenerationConfiguration
        {
            NamespaceDeclarationStyle = namespaceStyle
        };

        var result = StepDefinitionFileBuilder.BuildNewFile(
            snippets:     new[] { Snippet },
            className:    "Feature1StepDefinitions",
            @namespace:   "MyNamespace.MyProject",
            csharpConfig: csharpConfig,
            indent:       "    ",
            newLine:      Environment.NewLine);

        Approvals.Verify(result);
    }

    // ── Naming helpers ────────────────────────────────────────────────────────

    [Theory]
    [InlineData("addition.feature",        "AdditionStepDefinitions")]
    [InlineData("MyCalculator.feature",    "MyCalculatorStepDefinitions")]
    [InlineData("my-calculator.feature",   "MyCalculatorStepDefinitions")]
    [InlineData("feature with spaces.feature", "FeatureWithSpacesStepDefinitions")]
    public void ClassNameFromFeaturePath_derives_PascalCase_class(string fileName, string expected)
    {
        var className = StepDefinitionFileBuilder.ClassNameFromFeaturePath(
            Path.Combine("C:\\project\\features", fileName));
        className.Should().Be(expected);
    }

    [Fact]
    public void DeriveNamespace_appends_relative_folder_segments()
    {
        var ns = StepDefinitionFileBuilder.DeriveNamespace(
            projectFolder:    "C:\\project",
            defaultNamespace: "MyApp.Tests",
            targetFilePath:   "C:\\project\\Features\\AdditionStepDefinitions.cs");

        ns.Should().Be("MyApp.Tests.Features");
    }

    [Fact]
    public void DeriveNamespace_returns_default_when_file_is_at_project_root()
    {
        var ns = StepDefinitionFileBuilder.DeriveNamespace(
            projectFolder:    "C:\\project",
            defaultNamespace: "MyApp.Tests",
            targetFilePath:   "C:\\project\\AdditionStepDefinitions.cs");

        ns.Should().Be("MyApp.Tests");
    }
}
