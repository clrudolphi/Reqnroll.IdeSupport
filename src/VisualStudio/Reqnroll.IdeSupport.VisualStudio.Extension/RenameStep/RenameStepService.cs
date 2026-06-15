#nullable enable

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

    private readonly LspInterception.LspInterceptingPipe _pipe;
    private readonly IDeveroomLogger _fileLogger = new SynchronousFileLogger();

    public RenameStepService(LspInterception.LspInterceptingPipe pipe)
    {
        _pipe = pipe;
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
        _fileLogger.LogInfo($"RenameStepService: sending {RenameTargetsMethod} at {fileUri}:{line0}:{char0}");

        var result = await _pipe
            .SendRequestToServerAsync(RenameTargetsMethod, paramsJson, cancellationToken)
            .ConfigureAwait(false);

        if (result is JObject obj && obj["targets"] is JArray)
        {
            _fileLogger.LogInfo($"RenameStepService: got {obj["targets"]!.Value<JArray>()!.Count} target(s)");
            return obj;
        }

        _fileLogger.LogInfo("RenameStepService: no targets returned");
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
        _fileLogger.LogInfo($"RenameStepService: sending {SelectRenameTargetMethod} for attrIndex={attributeIndex}");

        await _pipe
            .SendNotificationToServerAsync(SelectRenameTargetMethod, paramsJson, cancellationToken)
            .ConfigureAwait(false);
    }

    private static string BuildPositionParams(string fileUri, int line0, int char0)
    {
        var escapedUri = Newtonsoft.Json.JsonConvert.ToString(fileUri);
        return $"{{\"textDocument\":{{\"uri\":{escapedUri}}},\"position\":{{\"line\":{line0},\"character\":{char0}}}}}";
    }

    private static string JsonEscape(string value) =>
        Newtonsoft.Json.JsonConvert.ToString(value);
}
