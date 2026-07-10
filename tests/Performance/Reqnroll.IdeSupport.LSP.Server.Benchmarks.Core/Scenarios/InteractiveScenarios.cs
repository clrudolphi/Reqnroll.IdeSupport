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
using Range = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;

namespace Reqnroll.IdeSupport.LSP.Server.Benchmarks.Scenarios;

/// <summary>
/// The interactive round-trip performance scenarios, measured end-to-end over the real LSP transport.
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
    /// Semantic tokens delta pull. The server doesn't maintain real delta state (see
    /// <c>SemanticTokensHandler</c> — it always returns the full token set wrapped in a
    /// <c>SemanticTokensFullOrDelta</c>), but this still exercises the delta wire shape and the
    /// handler's own dispatch/serialization cost, which the plain "full" scenario never reaches.
    /// A <c>previousResultId</c> is fetched once per feature outside the timed window (an untimed
    /// setup step, not part of what's measured).
    /// </summary>
    public async Task<LatencySummary> SemanticTokensDeltaAsync()
    {
        var resultIds = new string[_features.Count];
        for (var i = 0; i < _features.Count; i++)
        {
            var full = await _harness.RequestSemanticTokensAsync(_features[i].Uri).ConfigureAwait(false);
            resultIds[i] = full?.ResultId ?? "";
        }

        return await RunAsync(PerfTargets.SemanticTokensDelta.Operation, async i =>
        {
            var f = _features[i % _features.Count];
            await _harness.RequestSemanticTokensDeltaAsync(f.Uri, resultIds[i % _features.Count])
                .ConfigureAwait(false);
        }).ConfigureAwait(false);
    }

    /// <summary>
    /// <c>textDocument/prepareRename</c> at a bound step position — the validity check that fires
    /// before a client shows the rename box (F16).
    /// </summary>
    public async Task<LatencySummary> PrepareRenameAsync()
        => await RunAsync(PerfTargets.StepPrepareRename.Operation, async i =>
        {
            var f = _features[i % _features.Count];
            var (line, character) = f.StepPosition;
            await _harness.RequestPrepareRenameAsync(f.Uri, line, character).ConfigureAwait(false);
        }).ConfigureAwait(false);

    /// <summary>
    /// <c>reqnroll/renameTargets</c> at a bound step position — the picker candidates request that
    /// precedes rename when the cursor position resolves to more than one renameable attribute (F16).
    /// </summary>
    public async Task<LatencySummary> RenameTargetsAsync()
        => await RunAsync(PerfTargets.RenameTargets.Operation, async i =>
        {
            var f = _features[i % _features.Count];
            var (line, character) = f.StepPosition;
            await _harness.RequestRenameTargetsAsync(f.Uri, line, character).ConfigureAwait(false);
        }).ConfigureAwait(false);

    // ── References / go-to (F5/F17) ─────────────────────────────────────────────

    public async Task<LatencySummary> FindStepUsagesAsync()
        => await RunAsync(PerfTargets.FindStepUsages.Operation, async i =>
        {
            var f = _features[i % _features.Count];
            var (line, character) = f.StepPosition;
            await _harness.RequestFindStepUsagesAsync(f.Uri, line, character).ConfigureAwait(false);
        }).ConfigureAwait(false);

    public async Task<LatencySummary> StepReferencesAsync()
        => await RunAsync(PerfTargets.StepReferences.Operation, async i =>
        {
            var f = _features[i % _features.Count];
            var (line, character) = f.StepPosition;
            await _harness.RequestStepReferencesAsync(f.Uri, line, character).ConfigureAwait(false);
        }).ConfigureAwait(false);

    public async Task<LatencySummary> GoToStepDefinitionsAsync()
        => await RunAsync(PerfTargets.GoToStepDefinitions.Operation, async i =>
        {
            var f = _features[i % _features.Count];
            var (line, character) = f.StepPosition;
            await _harness.RequestGoToStepDefinitionsAsync(f.Uri, line, character).ConfigureAwait(false);
        }).ConfigureAwait(false);

    /// <summary>
    /// <c>reqnroll/goToHooks</c> — the corpus has no <c>[Before/AfterScenario]</c> hook bindings (see
    /// <c>CorpusGenerator.BuildBindings</c>), so this always exercises the "no hooks found" fast path
    /// rather than a populated result; still real protocol-boundary dispatch cost, same precedent as
    /// <see cref="RenameTargetsAsync"/> on a single-binding position.
    /// </summary>
    public async Task<LatencySummary> GoToHooksAsync()
        => await RunAsync(PerfTargets.GoToHooks.Operation, async i =>
        {
            var f = _features[i % _features.Count];
            var (line, character) = f.StepPosition;
            await _harness.RequestGoToHooksAsync(f.Uri, line, character).ConfigureAwait(false);
        }).ConfigureAwait(false);

    // ── Code lens (F18), inlay hints (F23), code actions (F6) ───────────────────

    public async Task<LatencySummary> InlayHintAsync()
        => await RunAsync(PerfTargets.InlayHint.Operation, async i =>
        {
            var f = _features[i % _features.Count];
            await _harness.RequestInlayHintAsync(f.Uri, f.FirstScenarioRange).ConfigureAwait(false);
        }).ConfigureAwait(false);

    /// <summary>
    /// <c>textDocument/codeAction</c> at the corpus's one deliberately-undefined step per scenario
    /// (see <c>CorpusGenerator.BuildFeature</c>) — the F6 quick-fix scaffold target.
    /// </summary>
    public async Task<LatencySummary> CodeActionAsync()
        => await RunAsync(PerfTargets.CodeAction.Operation, async i =>
        {
            var f = _features[i % _features.Count];
            await _harness.RequestCodeActionAsync(f.Uri, f.UndefinedStepRange).ConfigureAwait(false);
        }).ConfigureAwait(false);

    /// <summary>
    /// <c>textDocument/codeLens</c> on the corpus's .cs binding source file — <c>StepCodeLensHandler</c>
    /// only returns lenses for .cs files, not .feature files (see <c>CorpusGenerator.BuildBindings</c>).
    /// Opens the file once (untimed settle) then measures the lens request in isolation.
    /// </summary>
    public async Task<LatencySummary> StepCodeLensAsync(string corpusRoot)
    {
        var csPath = Path.Combine(corpusRoot, "Bindings", "CorpusSteps.cs");
        var uri = DocumentUri.FromFileSystemPath(csPath);
        _harness.OpenCSharp(uri, 1, File.ReadAllText(csPath));
        await Task.Delay(200).ConfigureAwait(false);

        return await RunAsync(PerfTargets.StepCodeLens.Operation, async _ =>
        {
            await _harness.RequestCodeLensAsync(uri).ConfigureAwait(false);
        }).ConfigureAwait(false);
    }

    // ── Formatting (F11/F12) ─────────────────────────────────────────────────────

    public async Task<LatencySummary> DocumentFormattingAsync()
        => await RunAsync(PerfTargets.DocumentFormatting.Operation, async i =>
        {
            var f = _features[i % _features.Count];
            await _harness.RequestDocumentFormattingAsync(f.Uri).ConfigureAwait(false);
        }).ConfigureAwait(false);

    public async Task<LatencySummary> RangeFormattingAsync()
        => await RunAsync(PerfTargets.RangeFormatting.Operation, async i =>
        {
            var f = _features[i % _features.Count];
            await _harness.RequestRangeFormattingAsync(f.Uri, f.FirstScenarioRange).ConfigureAwait(false);
        }).ConfigureAwait(false);

    /// <summary>
    /// <c>textDocument/onTypeFormatting</c> triggered by "|" at the end of one of the corpus's
    /// Examples table rows — the only "|"-delimited lines in the generated corpus (see
    /// <c>CorpusGenerator.BuildFeature</c>'s Scenario Outline block).
    /// </summary>
    public async Task<LatencySummary> OnTypeFormattingAsync()
        => await RunAsync(PerfTargets.OnTypeFormatting.Operation, async i =>
        {
            var f = _features[i % _features.Count];
            var (line, character) = f.ExamplesRowEndPosition;
            await _harness.RequestOnTypeFormattingAsync(f.Uri, line, character, "|").ConfigureAwait(false);
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

    /// <summary>A small range spanning the first scenario's step block (for range formatting / inlay hints).</summary>
    public Range FirstScenarioRange
    {
        get { var (l, _) = FirstStep(); return new Range(new Position(l, 0), new Position(l + 6, 0)); }
    }

    // The corpus's one deliberately-undefined step per scenario, "When undefined step {f}-{s} occurs"
    // (see CorpusGenerator.BuildFeature) — the F6 code-action / scaffold anchor.
    private (int Line, int Col)? _undefinedStep;

    private (int Line, int Col) UndefinedStep()
    {
        if (_undefinedStep is { } cached) return cached;
        var lines = Text.Replace("\r\n", "\n").Split('\n');
        for (var i = 0; i < lines.Length; i++)
        {
            var trimmed = lines[i].TrimStart();
            const string prefix = "When undefined step ";
            if (trimmed.StartsWith(prefix, StringComparison.Ordinal))
            {
                var indent = lines[i].Length - trimmed.Length;
                _undefinedStep = (i, indent);
                return _undefinedStep.Value;
            }
        }
        _undefinedStep = (0, 0);
        return _undefinedStep.Value;
    }

    /// <summary>Partway into the undefined step's text (for F6 code actions).</summary>
    public (int Line, int Character) UndefinedStepPosition
    {
        get { var (l, c) = UndefinedStep(); return (l, c + "When undef".Length); }
    }

    /// <summary>The whole undefined-step line (for the F6 code-action request range).</summary>
    public Range UndefinedStepRange
    {
        get { var (l, _) = UndefinedStep(); return new Range(new Position(l, 0), new Position(l + 1, 0)); }
    }

    // The corpus's Scenario Outline "Examples:" table (see CorpusGenerator.BuildFeature) — the only
    // "|"-delimited lines in the generated corpus, used as the F12 on-type-formatting trigger anchor.
    private (int Line, int EndCol)? _examplesRow;

    private (int Line, int EndCol) ExamplesRow()
    {
        if (_examplesRow is { } cached) return cached;
        var lines = Text.Replace("\r\n", "\n").Split('\n');
        for (var i = 0; i < lines.Length; i++)
        {
            var trimmed = lines[i].Trim();
            if (trimmed.Length > 1 && trimmed.StartsWith("|", StringComparison.Ordinal) &&
                trimmed.EndsWith("|", StringComparison.Ordinal))
            {
                _examplesRow = (i, lines[i].Length);
                return _examplesRow.Value;
            }
        }
        _examplesRow = (0, 0);
        return _examplesRow.Value;
    }

    /// <summary>End of an Examples table row, as if "|" was just typed there (for F12 on-type formatting).</summary>
    public (int Line, int Character) ExamplesRowEndPosition
    {
        get { var (l, c) = ExamplesRow(); return (l, c); }
    }
}
