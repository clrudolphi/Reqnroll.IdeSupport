#nullable enable

using System;
using System.Collections.Generic;
using System.IO;

namespace Reqnroll.IdeSupport.LSP.Server.Benchmarks.Harness;

/// <summary>
/// Locates the built corpus bindings assembly (<c>Reqnroll.IdeSupport.LSP.Server.Benchmarks.Corpus</c>,
/// compiled from the committed <c>tests/Performance/Corpus/Bindings</c> source) so the
/// Roslyn/reflection binding-discovery batch scenarios and the bound-state interactive benchmarks
/// can run against a primed registry instead of being skipped / unprimed. See
/// docs/Performance-Verification-Implementation-Plan.md, A3.2.
/// </summary>
public static class CorpusAssemblyLocator
{
    public const string AssemblyFileName = "Reqnroll.IdeSupport.LSP.Server.Benchmarks.Corpus.dll";
    private const string SolutionFile = "Reqnroll.IdeSupport.slnx";

    /// <summary>
    /// Finds the built corpus assembly. Checks <see cref="AppContext.BaseDirectory"/> first — the
    /// benchmark's own <c>DeployDiscoveryAssets</c> MSBuild target copies it there, which is also
    /// where <c>OutProcReqnrollConnectorFactory</c> resolves the connector from, so a deployed
    /// copy is the one binding discovery will actually use. Falls back to the corpus project's own
    /// <c>bin/&lt;config&gt;/net10.0</c> output (e.g. when running from a build that predates the
    /// deploy step). Returns <c>null</c> if not found (build the corpus project first) rather than
    /// throwing, so callers can fall back to reporting the discovery scenarios as skipped.
    /// </summary>
    public static string? TryFind()
    {
        var deployed = Path.Combine(AppContext.BaseDirectory, AssemblyFileName);
        if (File.Exists(deployed)) return deployed;

        string root;
        try { root = FindRepoRoot(); }
        catch (DirectoryNotFoundException) { return null; }

        var corpusBin = Path.Combine(
            root, "tests", "Performance", "Reqnroll.IdeSupport.LSP.Server.Benchmarks.Corpus", "bin");

        foreach (var config in PreferredConfigs())
        {
            var candidate = Path.Combine(corpusBin, config, "net10.0", AssemblyFileName);
            if (File.Exists(candidate)) return candidate;
        }

        return null;
    }

    // Prefer the configuration the benchmark itself was built in (inferred from its output path),
    // then fall back to the other.
    private static IEnumerable<string> PreferredConfigs()
    {
        var baseDir = AppContext.BaseDirectory.Replace('\\', '/');
        return baseDir.Contains("/Release/", StringComparison.OrdinalIgnoreCase)
            ? new[] { "Release", "Debug" }
            : new[] { "Debug", "Release" };
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, SolutionFile)))
                return dir.FullName;
            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException(
            $"Could not locate '{SolutionFile}' walking up from '{AppContext.BaseDirectory}'.");
    }
}
