#nullable enable

using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Reqnroll.IdeSupport.Common.Diagnostics;
using Reqnroll.IdeSupport.VisualStudio.Extension.LspInterception;

namespace Reqnroll.IdeSupport.VisualStudio.Extension.CommentToggle;

/// <summary>
/// Sends a <c>workspace/executeCommand</c> request for <c>reqnroll.toggleComment</c>
/// to the LSP server (F13 — Comment/Uncomment).
/// </summary>
/// <remarks>
/// The server responds with an acknowledgement and as a side-effect sends a
/// <c>workspace/applyEdit</c> notification back to the client, which VS's built-in LSP
/// infrastructure handles natively to apply the text edits.
/// </remarks>
internal sealed class CommentToggleService
{
    private const string ExecuteCommandMethod = "workspace/executeCommand";

    private readonly LspInterceptingPipe _pipe;
    private readonly TraceSource         _traceSource;
    private readonly IDeveroomLogger     _fileLogger = new SynchronousFileLogger();

    public CommentToggleService(LspInterceptingPipe pipe, TraceSource traceSource)
    {
        _pipe        = pipe;
        _traceSource = traceSource;
    }

    /// <summary>
    /// Sends a <c>workspace/executeCommand</c> request for <c>reqnroll.toggleComment</c>
    /// to toggle <c>#</c> comments on the selected lines (0-based, inclusive).
    /// </summary>
    public async Task ToggleCommentAsync(
        string            fileUri,
        int               startLine,
        int               endLine,
        CancellationToken cancellationToken)
    {
        var paramsJson = BuildParams(fileUri, startLine, endLine);

        _traceSource.TraceInformation(
            "CommentToggleService: sending workspace/executeCommand reqnroll.toggleComment " +
            "uri={0} lines[{1}..{2}]", fileUri, startLine, endLine);
        _fileLogger.LogInfo(
            $"CommentToggleService: sending reqnroll.toggleComment params={paramsJson}");

        var result = await _pipe
            .SendRequestToServerAsync(ExecuteCommandMethod, paramsJson, cancellationToken)
            .ConfigureAwait(false);

        _fileLogger.LogInfo(
            $"CommentToggleService: server response = {(result is null ? "<null>" : result.ToString())}");

        _traceSource.TraceInformation(
            "CommentToggleService: server acknowledged reqnroll.toggleComment");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string BuildParams(string fileUri, int startLine, int endLine)
    {
        var escapedUri = Newtonsoft.Json.JsonConvert.ToString(fileUri);
        return $"{{" +
               $"\"command\":\"reqnroll.toggleComment\"," +
               $"\"arguments\":[{escapedUri},{startLine},{endLine}]" +
               $"}}";
    }
}
