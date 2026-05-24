#nullable disable

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Reqnroll.IdeSupport.LSP.Core.Editor.Services.Parsing.GherkinDocuments;
using System.Text;
using System.Threading.Tasks;

namespace Reqnroll.IdeSupport.LSP.Core.Discovery;

public class StepDefinitionFileParser
{
    public async Task<List<ProjectStepDefinitionBinding>> Parse(CSharpStepDefinitionFile stepDefinitionFile)
    {
        var rootNode = await stepDefinitionFile.Content.GetRootAsync();

        var allMethods = rootNode
            .DescendantNodes()
            .OfType<MethodDeclarationSyntax>()
            .ToArray();

        var projectStepDefinitionBindings = new List<ProjectStepDefinitionBinding>(allMethods.Length);
        foreach (MethodDeclarationSyntax method in allMethods)
        {
            var attributes = GetAttributesWithTokens(method);

            var methodBodyBeginToken = method.Body.GetFirstToken();
            var methodBodyBeginPosition = methodBodyBeginToken.GetLocation().GetLineSpan().StartLinePosition;
            var methodBodyEndToken = method.Body.GetLastToken();
            var methodBodyEndPosition = methodBodyEndToken.GetLocation().GetLineSpan().StartLinePosition;

            Scope scope = null;
            var parameterTypes = method.ParameterList.Parameters
                .Select(p => p.Type.ToString())
                .ToArray();

            var sourceLocation = new SourceLocation(stepDefinitionFile.FullName,
                methodBodyBeginPosition.Line + 1,
                methodBodyBeginPosition.Character + 1,
                methodBodyEndPosition.Line + 1,
                methodBodyEndPosition.Character + 1);
            var implementation =
                new ProjectBindingImplementation(FullMethodName(method), parameterTypes, sourceLocation);

            foreach (var (attribute, token) in attributes)
            {
                var stepDefinitionType = (ScenarioBlock) Enum.Parse(typeof(ScenarioBlock), attribute.Name.ToString());
                var regex = new Regex($"^{token.ValueText}$");

                var stepDefinitionBinding = new ProjectStepDefinitionBinding(stepDefinitionType, regex, scope,
                    implementation, token.ValueText);

                projectStepDefinitionBindings.Add(stepDefinitionBinding);
            }
        }

        return projectStepDefinitionBindings;
    }

    private static string FullMethodName(MethodDeclarationSyntax method)
    {
        StringBuilder sb = new StringBuilder();
        var containingClass = method.Parent as ClassDeclarationSyntax;
        if (containingClass.Parent is BaseNamespaceDeclarationSyntax namespaceSyntax)
        {
            var containingNamespace = namespaceSyntax.Name;
            sb.Append(containingNamespace).Append('.');
        }

        sb.Append(containingClass.Identifier.Text).Append('.').Append(method.Identifier.Text);
        return sb.ToString();
    }
    internal static IEnumerable<(AttributeSyntax attribute, SyntaxToken token)> GetAttributesWithTokens(
    MethodDeclarationSyntax method)
    {
        return method.AttributeLists
            .SelectMany(al => al.Attributes)
            .Select(a => (a, GetAttributeToken(a)));
    }

    private static SyntaxToken GetAttributeToken(AttributeSyntax attributeSyntax)
    {
        AttributeArgumentListSyntax? attributeArgumentListSyntax = attributeSyntax.ArgumentList;
        return attributeArgumentListSyntax == null || attributeArgumentListSyntax.Arguments.Count == 0
            ? SyntaxFactory.MissingToken(SyntaxKind.StringLiteralToken)
            : attributeArgumentListSyntax.Arguments.Single().Expression.GetFirstToken();
    }
}
