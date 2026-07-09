#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Reqnroll.IdeSupport.VisualStudio.Extension.LspInterception;
using Reqnroll.IdeSupport.VisualStudio.NavigationBar;

namespace Reqnroll.IdeSupport.VisualStudio.Extension.NavigationBar;

/// <summary>
/// Fetches the Feature/Scenario/Step symbol tree for the Navigation Bar (Issue #5 / Q22 Option B)
/// by sending the custom <c>reqnroll/documentSymbolHierarchical</c> request over the owned
/// <see cref="LspInterceptingPipe"/>.
/// </summary>
/// <remarks>
/// Deliberately not <c>textDocument/documentSymbol</c>: that handler's response shape depends on
/// whether the real LSP client (VS) declared <c>hierarchicalDocumentSymbolSupport</c> — which VS
/// does not — so it can return either nested <c>DocumentSymbol</c> or flat <c>SymbolInformation</c>
/// depending on that capability, per-handler-instance rather than per-request. This service's
/// <see cref="MapResult"/> is written for the nested shape (reads <c>range</c>/<c>selectionRange</c>/
/// <c>children</c> directly), so it uses the always-hierarchical custom method instead of being at
/// the mercy of what VS's client capability happens to be.
/// </remarks>
internal sealed class GherkinNavigationBarSymbolService
{
    private const string RequestMethod = "reqnroll/documentSymbolHierarchical";

    private readonly LspInterceptingPipe _pipe;
    private readonly ILogger<GherkinNavigationBarSymbolService> _logger;

    public GherkinNavigationBarSymbolService(LspInterceptingPipe pipe, ILogger<GherkinNavigationBarSymbolService> logger)
    {
        _pipe   = pipe;
        _logger = logger;
    }

    public async Task<IReadOnlyList<GherkinSymbolNode>> FetchSymbolsAsync(
        string fileUri, CancellationToken cancellationToken)
    {
        var paramsJson = BuildParams(fileUri);

        _logger.LogInformation(
            "GherkinNavigationBarSymbolService: querying {RequestMethod} for {FileUri}", RequestMethod, fileUri);

        var result = await _pipe
            .SendRequestToServerAsync(RequestMethod, paramsJson, cancellationToken)
            .ConfigureAwait(false);

        var mapped = MapResult(result as JArray);

        _logger.LogInformation(
            "GherkinNavigationBarSymbolService: uri={FileUri} mapped {SymbolCount} top-level symbol(s).",
            fileUri, mapped.Count);

        return mapped;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string BuildParams(string fileUri)
    {
        var escapedUri = Newtonsoft.Json.JsonConvert.ToString(fileUri);
        return $"{{\"textDocument\":{{\"uri\":{escapedUri}}}}}";
    }

    internal static IReadOnlyList<GherkinSymbolNode> MapResult(JArray? array)
    {
        if (array is null || array.Count == 0)
            return Array.Empty<GherkinSymbolNode>();

        var result = new List<GherkinSymbolNode>(array.Count);
        foreach (var item in array)
        {
            if (item is JObject obj)
                result.Add(MapNode(obj));
        }
        return result;
    }

    private static GherkinSymbolNode MapNode(JObject obj)
    {
        var name = obj["name"]?.Value<string>() ?? string.Empty;
        var kind = obj["kind"]?.Value<int>() ?? 0;
        var range = MapRange(obj["range"] as JObject);
        var selectionRange = MapRange(obj["selectionRange"] as JObject);
        var children = MapResult(obj["children"] as JArray);

        return new GherkinSymbolNode(name, kind, range, selectionRange, children);
    }

    private static GherkinSymbolRange MapRange(JObject? range)
    {
        if (range is null)
            return default;

        return new GherkinSymbolRange(
            MapPosition(range["start"] as JObject),
            MapPosition(range["end"] as JObject));
    }

    private static GherkinSymbolPosition MapPosition(JObject? position)
    {
        if (position is null)
            return default;

        return new GherkinSymbolPosition(
            position["line"]?.Value<int>() ?? 0,
            position["character"]?.Value<int>() ?? 0);
    }
}
