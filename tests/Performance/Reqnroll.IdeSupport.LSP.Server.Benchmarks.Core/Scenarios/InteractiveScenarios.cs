#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Reqnroll.IdeSupport.LSP.Server.Benchmarks.Harness;
using Reqnroll.IdeSupport.LSP.Server.Benchmarks.Latency;

namespace Reqnroll.IdeSupport.LSP.Server.Benchmarks.Scenarios;

/// <summary>
/// The §9 interactive round-trip scenarios, measured end-to-end over the real LSP transport.
/// Each scenario warms the server, then collects per-invocation latency samples cycling across
/// multiple corpus documents (so the numbers reflect the corpus, not one hot file).
/// </summary>
public sealed class InteractiveScenarios
{
    private readonly BenchmarkLspHarness _harness;
    private readonly IReadOnlyList<OpenFeature> _features;
    private readonly int _warmup;
    private readonly int _measured;

    public InteractiveScenarios(
        BenchmarkLspHarness harness, IReadOnlyList<OpenFeature> features, int warmup, int measured)
    {
        _harness = harness;
        _features = features;
        _warmup = warmup;
        _measured = measured;
    }

    /// <summary>Opens <paramref name="count"/> corpus feature files and waits for them to parse.</summary>
    public static async Task<IReadOnlyList<OpenFeature>> OpenFeaturesAsync(
        BenchmarkLspHarness harness, string corpusRoot, int count)
    {
        var paths = Directory
            .EnumerateFiles(Path.Combine(corpusRoot, "Features"), "*.feature", SearchOption.AllDirectories)
            .OrderBy(p => p, StringComparer.Ordinal)
            .Take(count)
            .ToArray();

        var opened = new List<OpenFeature>();
        foreach (var path in paths)
        {
            var text = File.ReadAllText(path);
            var uri = DocumentUri.FromFileSystemPath(path);
            harness.OpenFeature(uri, 1, text);
            opened.Add(new OpenFeature(uri, text));
        }

        // Wait until at least the first file yields semantic tokens (server parses asynchronously).
        var deadline = DateTime.UtcNow.AddSeconds(10);
        while (DateTime.UtcNow < deadline)
        {
            var tokens = await harness.RequestAsync<SemanticTokens?>(
                "textDocument/semanticTokens/full",
                new SemanticTokensParams { TextDocument = new TextDocumentIdentifier { Uri = opened[0].Uri } })
                .ConfigureAwait(false);
            if (tokens is { Data.Length: > 0 }) break;
            await Task.Delay(50).ConfigureAwait(false);
        }

        return opened;
    }

    public async Task<LatencySummary> SemanticTokensAsync()
        => await RunAsync(PerfTargets.SemanticTokensFull.Operation, async i =>
        {
            var f = _features[i % _features.Count];
            await _harness.TimeRequestAsync<SemanticTokens?>(
                "textDocument/semanticTokens/full",
                new SemanticTokensParams { TextDocument = new TextDocumentIdentifier { Uri = f.Uri } })
                .ConfigureAwait(false);
        }).ConfigureAwait(false);

    public Task<LatencySummary> KeywordCompletionAsync()
        => CompletionAsync(PerfTargets.CompletionKeyword.Operation, keyword: true);

    public Task<LatencySummary> StepCompletionAsync()
        => CompletionAsync(PerfTargets.CompletionStep.Operation, keyword: false);

    private async Task<LatencySummary> CompletionAsync(string operation, bool keyword)
        => await RunAsync(operation, async i =>
        {
            var f = _features[i % _features.Count];
            // Keyword completion fires at the start of a scenario-body line; step completion fires
            // partway through a step's text. Both target a step line in the first scenario.
            var (line, character) = keyword ? f.KeywordPosition : f.StepPosition;
            await _harness.RequestAsync<CompletionList?>(
                "textDocument/completion",
                new CompletionParams
                {
                    TextDocument = new TextDocumentIdentifier { Uri = f.Uri },
                    Position = new Position(line, character),
                }).ConfigureAwait(false);
        }).ConfigureAwait(false);

    public async Task<LatencySummary> DefinitionAsync()
        => await RunAsync(PerfTargets.DefinitionCacheHit.Operation, async i =>
        {
            var f = _features[i % _features.Count];
            var (line, character) = f.StepPosition;
            await _harness.RequestAsync<LocationOrLocationLinks?>(
                "textDocument/definition",
                new DefinitionParams
                {
                    TextDocument = new TextDocumentIdentifier { Uri = f.Uri },
                    Position = new Position(line, character),
                }).ConfigureAwait(false);
        }).ConfigureAwait(false);

    /// <summary>
    /// Measures the diagnostics push: edit a feature (introducing a changed step) and time from the
    /// didChange to the publishDiagnostics for that URI.
    /// </summary>
    public async Task<LatencySummary> DiagnosticsPushAsync()
    {
        var recorder = new LatencyRecorder(PerfTargets.PublishDiagnostics.Operation);
        var version = 2;

        for (var i = 0; i < _warmup + _measured; i++)
        {
            var f = _features[i % _features.Count];
            var edited = f.Text + $"\n  # benchmark edit {version}\n";
            var start = Stopwatch.GetTimestamp();
            _harness.ChangeFeature(f.Uri, version++, edited);
            var ms = await _harness.WaitForDiagnosticsAsync(f.Uri, start).ConfigureAwait(false);
            if (i >= _warmup && ms is not null) recorder.Add(ms.Value);
        }

        return recorder.Summarize();
    }

    private async Task<LatencySummary> RunAsync(string operation, Func<int, Task> invoke)
    {
        var recorder = new LatencyRecorder(operation);
        for (var i = 0; i < _warmup; i++) await invoke(i).ConfigureAwait(false);
        for (var i = 0; i < _measured; i++)
        {
            var start = Stopwatch.GetTimestamp();
            await invoke(i).ConfigureAwait(false);
            recorder.Add(Stopwatch.GetElapsedTime(start).TotalMilliseconds);
        }
        return recorder.Summarize();
    }
}

/// <summary>An opened corpus feature plus precomputed cursor positions for the scenarios.</summary>
public sealed record OpenFeature(DocumentUri Uri, string Text)
{
    // The generated corpus places the first scenario's first step ("Given precondition 0 is met")
    // a few lines in. Find the first "Given " line to anchor step/keyword positions robustly.
    private (int Line, int Col)? _firstStep;

    private (int Line, int Col) FirstStep()
    {
        if (_firstStep is { } cached) return cached;
        var lines = Text.Replace("\r\n", "\n").Split('\n');
        for (var i = 0; i < lines.Length; i++)
        {
            var trimmed = lines[i].TrimStart();
            if (trimmed.StartsWith("Given ", StringComparison.Ordinal))
            {
                var indent = lines[i].Length - trimmed.Length;
                _firstStep = (i, indent);
                return _firstStep.Value;
            }
        }
        _firstStep = (0, 0);
        return _firstStep.Value;
    }

    /// <summary>Start of the step keyword (for F7 keyword completion).</summary>
    public (int Line, int Character) KeywordPosition
    {
        get { var (l, c) = FirstStep(); return (l, c); }
    }

    /// <summary>Partway into the step text (for F8 step completion / definition).</summary>
    public (int Line, int Character) StepPosition
    {
        get { var (l, c) = FirstStep(); return (l, c + "Given prec".Length); }
    }
}
