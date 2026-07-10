using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Reqnroll.IdeSupport.Common;

namespace Reqnroll.IdeSupport.LSP.Core.Bindings;

/// <summary>A step definition source file with its content already parsed into a Roslyn <see cref="SyntaxTree"/>.</summary>
public record CSharpStepDefinitionFile(FileDetails StepDefinitionPath, SyntaxTree Content)
    : StepDefinitionFile(StepDefinitionPath, Content);

/// <summary>A source file that may contain step definitions/hooks, paired with its parsed content.</summary>
public record StepDefinitionFile : FileDetails
{
    /// <summary>Creates a step definition file from its file details and parsed content.</summary>
    public StepDefinitionFile(FileDetails fileDetails, SyntaxTree content)
        : base(fileDetails)
    {
        Content = content;
    }

    /// <summary>The parsed C# syntax tree for this file's content.</summary>
    public SyntaxTree Content { get; init; }
}

/// <summary>Convenience extensions for attaching parsed C# content to a <see cref="FileDetails"/>.</summary>
public static class FileDetailsExtensions
{
    /// <summary>Parses <paramref name="content"/> as C# and wraps it with <paramref name="fileDetails"/> as a <see cref="CSharpStepDefinitionFile"/>.</summary>
    public static CSharpStepDefinitionFile WithCSharpContent(this FileDetails fileDetails, string content)
    {
        SyntaxTree treeContent = CSharpSyntaxTree.ParseText(content);
        return new CSharpStepDefinitionFile(fileDetails, treeContent);
    }
}
