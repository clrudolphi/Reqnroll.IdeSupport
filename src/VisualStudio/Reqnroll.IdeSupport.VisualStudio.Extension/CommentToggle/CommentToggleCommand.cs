#nullable enable

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.Extensibility.Commands;
using Microsoft.VisualStudio.Extensibility.Editor;
using Reqnroll.IdeSupport.Common.Diagnostics;

namespace Reqnroll.IdeSupport.VisualStudio.Extension.CommentToggle;

/// <summary>
/// "Comment/Uncomment" command — placed in the editor context menu,
/// visible only when a <c>.feature</c> file editor is active (design doc F13).
/// </summary>
/// <remarks>
/// When invoked on selected lines, sends <c>workspace/executeCommand</c> for
/// <c>reqnroll.toggleComment</c> to the LSP server. The server responds with a
/// <c>workspace/applyEdit</c> notification that VS applies natively.
/// </remarks>
[VisualStudioContribution]
internal sealed class CommentToggleCommand : Command
{
    // guidSHLMainMenu — VS shell main-menu set that owns all code-editor context-menu
    // groups.  Every working placement in this extension uses this GUID.
    private static readonly Guid GuidSHLMainMenu = new("{d309f791-903f-11d0-9efc-00a0c911004f}");

    // IDG_VS_CODEWIN_FINDREF (vsshlids.h 0x02B1) — Navigate group in the code-editor
    // context menu.  VS surfaces this group for LSP-owned documents; the Edit group
    // (0x02AD) is not shown for LSP files.  All other working Reqnroll commands
    // (GoToHooks, GoToDefinition, FindStepUsages) also use this group.
    private const int IDG_VS_CODEWIN_FINDREF = 0x02B1;

    private readonly CommentToggleState _state;
    private readonly TraceSource        _traceSource;
    private readonly IDeveroomLogger    _fileLogger = new SynchronousFileLogger();

    public CommentToggleCommand(CommentToggleState state, TraceSource traceSource)
    {
        _state       = state;
        _traceSource = traceSource;
    }

    public override CommandConfiguration CommandConfiguration => new("Comment/Uncomment")
    {
        Icon        = new CommandIconConfiguration(ImageMoniker.Custom("ReqnrollIcon"), IconSettings.IconAndText),

        // Show only when a .feature file editor is active; invisible in all other editors.
        VisibleWhen = ActivationConstraint.EditorContentType("reqnroll-gherkin"),

        // Placed in the edit group of the code-editor context menu
        Placements  =
        [
            CommandPlacement.VsctParent(GuidSHLMainMenu, IDG_VS_CODEWIN_FINDREF, 0x0300),
        ],
    };

    public override async Task ExecuteCommandAsync(IClientContext context, CancellationToken cancellationToken)
    {
        try
        {
            _fileLogger.LogInfo("CommentToggleCommand: invoked.");

            var service = _state.Service;
            if (service is null)
            {
                _fileLogger.LogWarning("CommentToggleCommand: LSP server not yet initialized.");
                return;
            }

            var textView = await context.GetActiveTextViewAsync(cancellationToken).ConfigureAwait(false);
            if (textView is null)
            {
                _fileLogger.LogWarning("CommentToggleCommand: no active text view.");
                return;
            }

            var fileUri = textView.Uri.ToString();

            // Determine the line range from the selection.
            // If nothing is selected (collapsed selection), use the current line only.
            var selection = textView.Selection;
            var startPos  = selection.Start;
            var endPos    = selection.End;

            int startLine, endLine;
            if (startPos == endPos)
            {
                // No selection — use the current line.
                var line = startPos.GetContainingLine();
                startLine = line.LineNumber;
                endLine   = line.LineNumber;
                _fileLogger.LogInfo(
                    $"CommentToggleCommand: no selection — using current line {startLine}.");
            }
            else
            {
                startLine = startPos.GetContainingLine().LineNumber;
                endLine   = endPos.GetContainingLine().LineNumber;
                _fileLogger.LogInfo(
                    $"CommentToggleCommand: selection lines [{startLine}..{endLine}].");
            }

            _traceSource.TraceInformation(
                "CommentToggleCommand: uri='{0}', lines [{1}..{2}]",
                fileUri, startLine, endLine);

            await service
                .ToggleCommentAsync(fileUri, startLine, endLine, cancellationToken)
                .ConfigureAwait(false);

            _fileLogger.LogInfo("CommentToggleCommand: completed successfully.");
        }
        catch (Exception ex)
        {
            _fileLogger.LogWarning($"CommentToggleCommand: failed: {ex}");
            _traceSource.TraceEvent(TraceEventType.Error, 0, "CommentToggleCommand: failed: {0}", ex);
        }
    }
}
