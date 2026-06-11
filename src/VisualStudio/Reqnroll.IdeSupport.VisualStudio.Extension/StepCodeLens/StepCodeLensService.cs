#nullable enable

using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Reqnroll.IdeSupport.Common.Diagnostics;
using Reqnroll.IdeSupport.VisualStudio.Extension.LspInterception;

namespace Reqnroll.IdeSupport.VisualStudio.Extension.StepCodeLens;

/// <summary>
/// Sends a <c>textDocument/codeLens</c> request to the LSP server and maps the result to a
/// list of <see cref="StepLensItem"/> records (design doc F18).
/// </summary>
/// <remarks>
/// VS.Extensibility <c>ICodeLensProvider</c> calls this once per file refresh; the result is
/// shared across all code-element lenses in the same file so the LSP round-trip is amortised.
/// </remarks>
internal sealed class StepCodeLensService
{
    private const string RequestMethod = "textDocument/codeLens";

    private readonly LspInterceptingPipe _pipe;
    private readonly TraceSource         _traceSource;
    private readonly IDeveroomLogger     _fileLogger = new SynchronousFileLogger();

    public StepCodeLensService(LspInterceptingPipe pipe, TraceSource traceSource)
    {
        _pipe        = pipe;
        _traceSource = traceSource;
    }

    /// <summary>
    /// Queries the LSP server for all step-binding lenses in <paramref name="fileUri"/>.
    /// Returns an empty list when the file has no step-binding attributes or has not yet
    /// been discovered.
    /// </summary>
    public async Task<IReadOnlyList<StepLensItem>> GetLensesAsync(
        string            fileUri,
        CancellationToken cancellationToken)
    {
        var paramsJson = BuildParams(fileUri);

        _fileLogger.LogInfo($"StepCodeLensService: requesting {RequestMethod} for {fileUri}");
        _traceSource.TraceInformation("StepCodeLensService: requesting lenses for {0}", fileUri);

        var result = await _pipe
            .SendRequestToServerAsync(RequestMethod, paramsJson, cancellationToken)
            .ConfigureAwait(false);

        _fileLogger.LogInfo(
            $"StepCodeLensService: raw result = {(result is null ? "<null>" : result.ToString())}");

        if (result is null || result.Type == JTokenType.Null)
        {
            _traceSource.TraceInformation("StepCodeLensService: server returned null — no lenses");
            return System.Array.Empty<StepLensItem>();
        }

        if (result is JArray array)
        {
            var items = ParseItems(array);
            _traceSource.TraceInformation(
                "StepCodeLensService: {0} lens(es) returned for {1}", items.Count, fileUri);
            return items;
        }

        _traceSource.TraceInformation(
            "StepCodeLensService: unexpected result token type {0} for {1}", result.Type, fileUri);
        return System.Array.Empty<StepLensItem>();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string BuildParams(string fileUri)
    {
        var escapedUri = Newtonsoft.Json.JsonConvert.ToString(fileUri);
        return $"{{\"textDocument\":{{\"uri\":{escapedUri}}}}}";
    }

    private static IReadOnlyList<StepLensItem> ParseItems(JArray array)
    {
        var result = new List<StepLensItem>(array.Count);
        foreach (var token in array)
        {
            if (token is not JObject obj) continue;

            var rangeLine = obj["range"]?["start"]?["line"]?.Value<int>() ?? -1;
            if (rangeLine < 0) continue;

            var command     = obj["command"] as JObject;
            var title       = command?["title"]?.Value<string>() ?? string.Empty;
            var commandName = command?["name"]?.Value<string>() ?? string.Empty;

            // Arguments from the server: [fileUri, attrLine0, 0]
            var args         = command?["arguments"] as JArray;
            var argLine      = args?.Count >= 2 ? args[1].Value<int>() : rangeLine;

            result.Add(new StepLensItem(rangeLine, title, commandName, argLine));
        }
        return result;
    }
}

/// <summary>One code-lens item returned by the LSP server for a step-binding attribute.</summary>
internal sealed record StepLensItem(
    int    RangeLine,
    string Title,
    string CommandName,
    int    ArgLine);
