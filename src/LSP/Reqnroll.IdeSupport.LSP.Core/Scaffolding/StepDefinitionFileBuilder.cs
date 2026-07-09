#nullable enable

using System.IO;
using System.Text;
using Reqnroll.IdeSupport.Common.Configuration;
using Reqnroll.IdeSupport.Common.ProjectSystem;

namespace Reqnroll.IdeSupport.LSP.Core.Scaffolding;

/// <summary>
/// Assembles a complete C# step-definition source file from one or more rendered step snippets.
/// </summary>
public static class StepDefinitionFileBuilder
{
    /// <summary>
    /// Builds the full content of a new <c>.cs</c> step-definition file.
    /// </summary>
    /// <param name="snippets">
    /// Pre-rendered method snippets (each already indented at one level).
    /// Produced by <see cref="StepSkeletonRenderer.Render"/>.
    /// </param>
    /// <param name="className">The step-definition class name (e.g. <c>AdditionStepDefinitions</c>).</param>
    /// <param name="namespace">The target namespace.</param>
    /// <param name="csharpConfig">Controls block-scoped vs. file-scoped namespace style.</param>
    /// <param name="indent">The indentation unit (e.g. four spaces).</param>
    /// <param name="newLine">The line-ending string.</param>
    public static string BuildNewFile(
        IReadOnlyList<string>             snippets,
        string                            className,
        string                            @namespace,
        CSharpCodeGenerationConfiguration csharpConfig,
        string                            indent,
        string                            newLine)
    {
        bool fileScoped = csharpConfig.UseFileScopedNamespaces;

        var sb = new StringBuilder();

        // Using directives
        sb.Append("using System;").Append(newLine);
        sb.Append("using Reqnroll;").Append(newLine);
        sb.Append(newLine);

        if (fileScoped)
        {
            // file-scoped namespace: no braces, class at top level
            sb.Append($"namespace {@namespace};").Append(newLine);
            sb.Append(newLine);
            sb.Append("[Binding]").Append(newLine);
            sb.Append($"public class {className}").Append(newLine);
            sb.Append('{').Append(newLine);
            // Snippets are already pre-indented at one level; no extra prefix needed.
            AppendSnippets(sb, snippets, newLine, classIndent: "");
            sb.Append('}').Append(newLine);
        }
        else
        {
            // block-scoped namespace
            sb.Append($"namespace {@namespace}").Append(newLine);
            sb.Append('{').Append(newLine);
            sb.Append(indent).Append("[Binding]").Append(newLine);
            sb.Append(indent).Append($"public class {className}").Append(newLine);
            sb.Append(indent).Append('{').Append(newLine);
            // Snippets are pre-indented at one level; add one more for the class body.
            AppendSnippets(sb, snippets, newLine, classIndent: indent);
            sb.Append(indent).Append('}').Append(newLine);
            sb.Append('}').Append(newLine);
        }

        return sb.ToString();
    }

    private static void AppendSnippets(
        StringBuilder          sb,
        IReadOnlyList<string>  snippets,
        string                 newLine,
        string                 classIndent)
    {
        for (int i = 0; i < snippets.Count; i++)
        {
            // Normalize line endings so splitting works regardless of snippet origin.
            var normalized = snippets[i].Replace("\r\n", "\n").Replace("\r", "\n");
            var lines = normalized.Split('\n');

            // Trim trailing empty strings produced by a trailing newline in the snippet.
            int end = lines.Length;
            while (end > 0 && lines[end - 1].Length == 0)
                end--;

            for (int j = 0; j < end; j++)
            {
                var line = lines[j];
                if (line.Length == 0)
                    sb.Append(newLine);
                else
                    sb.Append(classIndent).Append(line).Append(newLine);
            }

            // Blank line between methods, but not after the last one.
            if (i < snippets.Count - 1)
                sb.Append(newLine);
        }
    }

    // ── File naming helpers ────────────────────────────────────────────────────

    /// <summary>
    /// Derives a step-definition class name from a feature file path.
    /// E.g. <c>addition.feature</c> → <c>AdditionStepDefinitions</c>.
    /// </summary>
    public static string ClassNameFromFeaturePath(string featureFilePath)
    {
        var baseName = Path.GetFileNameWithoutExtension(featureFilePath);
        return ToPascalCase(baseName) + "StepDefinitions";
    }

    /// <summary>
    /// Derives the target <c>.cs</c> file path from the feature file path.
    /// Prefers placing the file in a sibling <c>StepDefinitions/</c> directory if one exists.
    /// </summary>
    public static string TargetFilePath(string featureFilePath, string className)
    {
        var featureDir  = Path.GetDirectoryName(featureFilePath) ?? string.Empty;
        var stepDefsDir = Path.Combine(featureDir, "StepDefinitions");

        var targetDir = Directory.Exists(stepDefsDir) ? stepDefsDir : featureDir;
        return Path.Combine(targetDir, className + ".cs");
    }

    /// <summary>
    /// Derives a namespace from a project root, default namespace, and a target file path.
    /// </summary>
    public static string DeriveNamespace(
        string projectFolder,
        string defaultNamespace,
        string targetFilePath)
    {
        var targetDir = Path.GetDirectoryName(Path.GetFullPath(targetFilePath)) ?? string.Empty;
        var projFull  = Path.GetFullPath(projectFolder);

        if (!PathUtils.IsUnderFolder(targetDir, projFull))
            return defaultNamespace;

        var relative = targetDir.Substring(projFull.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        if (relative.Length == 0)
            return defaultNamespace;

        var nsSegments = relative
            .Split(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar })
            .Select(ToPascalCase)
            .Where(s => s.Length > 0);

        return defaultNamespace + "." + string.Join(".", nsSegments);
    }

    private static string ToPascalCase(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;
        var parts = System.Text.RegularExpressions.Regex.Split(s, @"[^a-zA-Z0-9]+")
                    .Where(p => p.Length > 0)
                    .Select(p => char.ToUpperInvariant(p[0]) + p.Substring(1));
        return string.Concat(parts);
    }
}
