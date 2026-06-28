#nullable enable

using System;
using System.Collections.Generic;
using System.IO;

namespace Reqnroll.IdeSupport.LSP.Server.Benchmarks.Harness;

/// <summary>
/// Locates the built, self-contained LSP server executable so the benchmark can spawn it
/// out-of-process over stdio (the production transport), as an alternative to hosting it
/// in-process over an in-memory pipe.
/// </summary>
public static class ServerExeLocator
{
    public const string ServerExeName = "Reqnroll.IdeSupport.LSP.Server.exe";
    private const string SolutionFile = "Reqnroll.IdeSupport.slnx";

    /// <summary>
    /// Finds the server exe under <c>src/LSP/.../bin/&lt;config&gt;/net10.0/win-x64</c>, preferring the
    /// configuration this benchmark was built in. Throws if not found (build the server first).
    /// </summary>
    public static string Find()
    {
        var root = FindRepoRoot();
        var serverBin = Path.Combine(root, "src", "LSP", "Reqnroll.IdeSupport.LSP.Server", "bin");

        foreach (var config in PreferredConfigs())
        {
            var candidate = Path.Combine(serverBin, config, "net10.0", "win-x64", ServerExeName);
            if (File.Exists(candidate)) return candidate;
        }

        throw new FileNotFoundException(
            $"Could not find {ServerExeName} under '{serverBin}' (Debug/Release). Build the server project first.");
    }

    public static bool TryFind(out string path)
    {
        try { path = Find(); return true; }
        catch (Exception ex) when (ex is FileNotFoundException or DirectoryNotFoundException)
        {
            path = string.Empty;
            return false;
        }
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
