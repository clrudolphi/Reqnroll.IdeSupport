#nullable enable

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Reqnroll.IdeSupport.LSP.Server.Benchmarks.Corpus;

namespace Reqnroll.IdeSupport.LSP.Server.Benchmarks;

/// <summary>
/// Entry point for the Performance Verification Layer 2 tooling.
/// <list type="bullet">
///   <item><c>run</c> — run the benchmark suite against the committed corpus (default).</item>
///   <item><c>generate-corpus</c> — (re)generate the committed corpus and rewrite its manifest.</item>
///   <item><c>--help</c> — describe the commands, options, and corpus regeneration.</item>
/// </list>
/// </summary>
public static class Program
{
    /// <summary>How this tool is invoked, used in the help text's examples.</summary>
    private const string Invocation =
        "dotnet run --project tests/Performance/Reqnroll.IdeSupport.LSP.Server.Benchmarks --";

    public static async Task<int> Main(string[] args)
    {
        if (IsHelpRequested(args))
        {
            PrintHelp();
            return 0;
        }

        var command = args.Length > 0 ? args[0] : "run";
        switch (command)
        {
            case "generate-corpus":
                return await GenerateCorpusAsync(args).ConfigureAwait(false);
            case "run":
                return await BenchmarkRunner.RunAsync(args).ConfigureAwait(false);
            default:
                Console.Error.WriteLine($"Unknown command '{command}'. Run with --help for usage.");
                return 2;
        }
    }

    private static bool IsHelpRequested(string[] args) =>
        args.Any(a =>
            a is "--help" or "-h" or "-?" or "/?" ||
            string.Equals(a, "help", StringComparison.OrdinalIgnoreCase));

    private static void PrintHelp()
    {
        Console.WriteLine($"""
            Reqnroll LSP performance benchmarks — Performance Verification, Layer 2.

            Hosts the real LSP server in-process and measures per-operation latency against the
            architecture's Performance Requirements, using the pinned corpus under
            tests/Performance/Corpus.

            USAGE
              {Invocation} <command> [options]

            COMMANDS
              run                 Run the benchmark suite against the committed corpus (default
                                  when no command is given).
              generate-corpus     Regenerate the committed corpus and rewrite its manifest
                                  (re-pin). See "REGENERATING THE CORPUS" below.
              --help, -h          Show this help.

            'run' OPTIONS
              --warmup <n>           Discarded warm-up iterations per interactive op.   (default 10)
              --iterations <n>       Measured iterations per interactive op.            (default 50)
              --files <n>            Corpus feature files to open and drive.            (default 10)
              --out <path>           Write the results JSON (also the future regression-tracking
                                     baseline format) to <path>.
              --no-batch             Skip the batch scenarios (cold-start scan) for a quick run.
              --corpus-assembly <p>  Path to a BUILT corpus bindings assembly. Enables the
                                     binding-discovery batch scenarios; without it they are
                                     reported as skipped (never faked).
              --assert               Enforce the absolute targets and exit non-zero on any miss.
                                     Use ONLY on a designated reference machine — shared/CI
                                     hardware is too noisy for absolute pass/fail.

            ENVIRONMENT
              REQNROLL_PERF_REFERENCE_MACHINE=1   Marks this host as the reference machine; same
                                                  effect as --assert. Leave unset elsewhere.

            EXIT CODES
              0   success (also the result of a report-only run, whatever the numbers)
              1   asserting (reference machine / --assert) and a target was missed
              2   unknown command

            EXAMPLES
              {Invocation} run                      # report-only, quick local numbers
              {Invocation} run --files 25 --iterations 200 --out results.json
              {Invocation} run --assert             # enforce targets (reference machine)
              {Invocation} generate-corpus          # re-pin the corpus after a deliberate change

            REGENERATING THE CORPUS
              The corpus under tests/Performance/Corpus is a PINNED artifact: the committed files
              ARE the benchmark workload, and corpus.manifest.json records their structural
              fingerprint (feature/scenario/step counts, pattern count, and the
              bound/unbound/ambiguous mix). The corpus drift test asserts the two still match.

              Regenerate ONLY when you intend to change the corpus shape or size — e.g. you
              changed the generator parameters (feature count, unique-pattern count, scenarios
              per feature) or the generation logic itself. Then:

                {Invocation} generate-corpus

              That rewrites Features/, Bindings/, reqnroll.json and corpus.manifest.json. Review
              the diff and COMMIT it — committing the regenerated files is what re-pins the corpus.

              Do NOT regenerate just to make a failing drift test pass. A failing CorpusDriftTests
              means the committed corpus no longer matches its pinned fingerprint; find out why
              first, and only re-pin if the change was intended. Generation is deterministic (no
              randomness), so an unchanged generator reproduces byte-identical files.
            """);
    }

    private static async Task<int> GenerateCorpusAsync(string[] args)
    {
        var corpusRoot = TryGetCorpusRoot() ?? DefaultCorpusRoot();
        Directory.CreateDirectory(corpusRoot);

        var generator = new CorpusGenerator();
        Console.WriteLine($"Generating corpus at {corpusRoot} " +
            $"({generator.FeatureFileCount} features, {generator.UniquePatternCount} unique patterns)...");
        generator.Generate(corpusRoot);

        var fingerprint = await CorpusFingerprint.ComputeAsync(corpusRoot).ConfigureAwait(false);
        var manifest = new CorpusManifest(
            Description: "Synthetic benchmark corpus for Performance Verification (Layer 2 / T2). " +
                         "Pinned by the committed files; this manifest records the structural fingerprint.",
            Generator: new GeneratorParameters(
                generator.FeatureFileCount, generator.UniquePatternCount, generator.ScenariosPerFeature),
            Fingerprint: fingerprint);

        File.WriteAllText(CorpusLocator.ManifestPath(corpusRoot), manifest.ToJson() + Environment.NewLine);

        Console.WriteLine("Corpus regenerated. Fingerprint:");
        Console.WriteLine($"  feature files : {fingerprint.FeatureFileCount}");
        Console.WriteLine($"  scenarios     : {fingerprint.ScenarioCount} (+ {fingerprint.ScenarioOutlineCount} outlines)");
        Console.WriteLine($"  steps         : {fingerprint.StepCount}");
        Console.WriteLine($"  patterns      : {fingerprint.StepDefinitionPatternCount}");
        Console.WriteLine($"  bound         : {fingerprint.BoundStepCount}");
        Console.WriteLine($"  unbound       : {fingerprint.UnboundStepCount}");
        Console.WriteLine($"  ambiguous     : {fingerprint.AmbiguousStepCount}");
        return 0;
    }

    private static string? TryGetCorpusRoot()
    {
        try { return CorpusLocator.FindCorpusRoot(); }
        catch (DirectoryNotFoundException) { return null; }
    }

    // When the corpus does not yet exist (first generation), fall back to walking up to the repo
    // root from the assembly location and creating tests/Performance/Corpus there.
    private static string DefaultCorpusRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !Directory.Exists(Path.Combine(dir.FullName, "tests", "Performance")))
            dir = dir.Parent;
        var baseDir = dir?.FullName ?? Directory.GetCurrentDirectory();
        return Path.Combine(baseDir, "tests", "Performance", "Corpus");
    }
}
