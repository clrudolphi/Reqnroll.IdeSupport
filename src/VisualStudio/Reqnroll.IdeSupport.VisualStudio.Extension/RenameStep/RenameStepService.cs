#nullable enable

using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Reqnroll.IdeSupport.Common.Diagnostics;

namespace Reqnroll.IdeSupport.VisualStudio.Extension.RenameStep;

/// <summary>
/// Sends custom <c>reqnroll/renameTargets</c> and <c>reqnroll/selectRenameTarget</c>
/// requests over the <c>LspInterceptingPipe</c> for the F16 Step Rename feature.
/// </summary>
internal sealed class RenameStepService
{
    private const string RenameTargetsMethod = "reqnroll/renameTargets";
    private const string SelectRenameTargetMethod = "reqnroll/selectRenameTarget";
    private const string RenameMethod = "textDocument/rename";

    private readonly LspInterception.LspInterceptingPipe _pipe;
    private readonly TraceSource _traceSource;
    private readonly IDeveroomLogger _fileLogger = new SynchronousFileLogger();

    public RenameStepService(LspInterception.LspInterceptingPipe pipe, TraceSource traceSource)
    {
        _pipe = pipe;
        _traceSource = traceSource;
    }

    /// <summary>
    /// Queries the server for renameable binding targets at the given position.
    /// Returns a JObject with a "targets" array, or null if no targets.
    /// </summary>
    public async Task<JObject?> GetRenameTargetsAsync(
        string fileUri, int line0, int char0,
        CancellationToken cancellationToken)
    {
        var paramsJson = BuildPositionParams(fileUri, line0, char0);

        _traceSource.TraceInformation(
            "RenameStepService: querying {0} at {1}:{2}:{3}", RenameTargetsMethod, fileUri, line0, char0);

        var result = await _pipe
            .SendRequestToServerAsync(RenameTargetsMethod, paramsJson, cancellationToken)
            .ConfigureAwait(false);

        if (result is JObject obj && obj["targets"] is JArray arr && arr.Count > 0)
        {
            _traceSource.TraceInformation(
                "RenameStepService: {0} target(s) returned", arr.Count);
            return obj;
        }

        _traceSource.TraceInformation("RenameStepService: no targets returned");
        return null;
    }

    /// <summary>
    /// Tells the server to remember the selected attribute index for the next rename.
    /// </summary>
    public async Task SelectRenameTargetAsync(
        string fileUri, int version, int attributeIndex,
        CancellationToken cancellationToken)
    {
        var paramsJson = $"{{\"uri\":{JsonEscape(fileUri)},\"version\":{version},\"attributeIndex\":{attributeIndex}}}";
        _traceSource.TraceInformation(
            "RenameStepService: sending {0} for attrIndex={1}", SelectRenameTargetMethod, attributeIndex);

        await _pipe
            .SendNotificationToServerAsync(SelectRenameTargetMethod, paramsJson, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Sends the standard <c>textDocument/rename</c> request and returns the workspace edit
    /// result, or null if the server had no edit to return.
    /// </summary>
    public async Task<JToken?> SendRenameRequestAsync(
        string fileUri, int line0, int char0, string newName,
        CancellationToken cancellationToken)
    {
        var paramsJson = BuildRenameParams(fileUri, line0, char0, newName);
        _traceSource.TraceInformation(
            "RenameStepService: sending {0} at {1}:{2}:{3}", RenameMethod, fileUri, line0, char0);
        _fileLogger.LogInfo($"RenameStepService: sending {RenameMethod} at {fileUri}:{line0}:{char0}");

        var result = await _pipe
            .SendRequestToServerAsync(RenameMethod, paramsJson, cancellationToken)
            .ConfigureAwait(false);

        if (result is not null)
            _fileLogger.LogInfo($"RenameStepService: {RenameMethod} returned workspace edit");

        return result;
    }

    /// <summary>
    /// Sends a <c>textDocument/didChange</c> notification so the server re-parses a file
    /// that was modified by the rename workspace edit while closed.
    /// </summary>
    public Task SendDidChangeAsync(string localPath, string newContent, CancellationToken cancellationToken)
    {
        var escapedContent = Newtonsoft.Json.JsonConvert.ToString(newContent);
        var featureUri = "file:///" + localPath.Replace('\\', '/');
        var paramsJson = $"{{\"textDocument\":{{\"uri\":\"{featureUri}\",\"version\":1}},\"contentChanges\":[{{\"text\":{escapedContent}}}]}}";

        _traceSource.TraceInformation(
            "RenameStepService: sending textDocument/didChange for '{0}'", localPath);

        return _pipe.SendNotificationToServerAsync("textDocument/didChange", paramsJson, cancellationToken);
    }

    private static string BuildPositionParams(string fileUri, int line0, int char0)
    {
        var escapedUri = JsonEscape(fileUri);
        return $"{{\"textDocument\":{{\"uri\":{escapedUri}}},\"position\":{{\"line\":{line0},\"character\":{char0}}}}}";
    }

    private static string BuildRenameParams(string fileUri, int line0, int char0, string newName)
    {
        var escapedUri = JsonEscape(fileUri);
        var escapedNewName = JsonEscape(newName);
        return $"{{\"textDocument\":{{\"uri\":{escapedUri}}},\"position\":{{\"line\":{line0},\"character\":{char0}}},\"newName\":{escapedNewName}}}";
    }

    internal static string JsonEscape(string value) =>
        Newtonsoft.Json.JsonConvert.ToString(value);
}
