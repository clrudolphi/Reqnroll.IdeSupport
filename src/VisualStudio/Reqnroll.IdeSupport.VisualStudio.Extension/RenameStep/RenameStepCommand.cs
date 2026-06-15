#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnvDTE80;
using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.Extensibility.Commands;
using Microsoft.VisualStudio.Extensibility.Editor;
using Microsoft.VisualStudio.Shell;
using Newtonsoft.Json.Linq;
using Reqnroll.IdeSupport.Common.Diagnostics;
using Reqnroll.IdeSupport.VisualStudio.Extension.Navigation;

namespace Reqnroll.IdeSupport.VisualStudio.Extension.RenameStep;

/// <summary>
/// \"Rename Step\" command for the Visual Studio extension (F16 Step Rename Refactoring).
/// <para>
/// When invoked from a C# binding file, queries the server for renameable targets,
/// shows a picker if multiple targets exist, selects the target, and triggers VS's
/// native Rename (F2) to execute the actual rename through the standard LSP protocol.
/// </para>
/// </summary>
[VisualStudioContribution]
internal sealed class RenameStepCommand : Command
{
    private readonly RenameStepState _state;
    private readonly TraceSource _traceSource;
    private readonly IDeveroomLogger _fileLogger = new SynchronousFileLogger();

    private static readonly Guid GuidSHLMainMenu = new("{D309F791-903F-11D0-9EFC-00A0C911004F}");
    private const int IDG_VS_CODEWIN_NAVIGATETOLOCATION = 0x02B1;

    public RenameStepCommand(RenameStepState state, TraceSource traceSource)
    {
        _state = state;
        _traceSource = traceSource;
    }

    public override CommandConfiguration CommandConfiguration => new("Rename Step")
    {
        Icon = new CommandIconConfiguration(ImageMoniker.Custom("ReqnrollIcon"), IconSettings.IconAndText),
        VisibleWhen = ActivationConstraint.EditorContentType("CSharp"),
        Placements =
        [
            CommandPlacement.VsctParent(GuidSHLMainMenu, id: IDG_VS_CODEWIN_NAVIGATETOLOCATION, priority: 0x0100),
        ],
    };

    public override async Task ExecuteCommandAsync(IClientContext context, CancellationToken cancellationToken)
    {
        try
        {
            _fileLogger.LogInfo("RenameStepCommand: invoked.");

            var service = _state.Service;
            if (service is null)
            {
                _fileLogger.LogWarning("RenameStepCommand: LSP server not yet initialized.");
                VsUtils.ShowStatusBarMessage("Reqnroll: LSP server not yet initialized — open a .feature file to activate it.");
                return;
            }

            var textView = await context.GetActiveTextViewAsync(cancellationToken).ConfigureAwait(false);
            if (textView is null)
            {
                _fileLogger.LogWarning("RenameStepCommand: No active text view in client context.");
                return;
            }

            var fileUri  = textView.Uri.ToString();
            var caretPos = textView.Selection.ActivePosition;
            var line     = caretPos.GetContainingLine();
            var lineNum  = line.LineNumber;
            var charNum  = caretPos.Offset - line.Text.Start;

            _fileLogger.LogInfo(
                $"RenameStepCommand: active view uri='{fileUri}', caret line={lineNum} char={charNum}.");

            // ── Step 1: Get rename targets from the server ───────────────────
            var targets = await service.GetRenameTargetsAsync(fileUri, lineNum, charNum, cancellationToken)
                .ConfigureAwait(false);

            if (targets is null)
            {
                _fileLogger.LogInfo("RenameStepCommand: no renameable targets at cursor position.");
                VsUtils.ShowStatusBarMessage("Reqnroll: No step definition found to rename at this position.");
                return;
            }

            var targetsArray = targets["targets"] as JArray;
            if (targetsArray is null || targetsArray.Count == 0)
            {
                _fileLogger.LogInfo("RenameStepCommand: empty targets array.");
                VsUtils.ShowStatusBarMessage("Reqnroll: No step definition found to rename at this position.");
                return;
            }

            // ── Step 2: Select target (picker if multiple) ──────────────────
            int selectedAttributeIndex;

            if (targetsArray.Count == 1)
            {
                selectedAttributeIndex = targetsArray[0]["attributeIndex"]?.Value<int>() ?? 0;
                _fileLogger.LogInfo($"RenameStepCommand: single target, attributeIndex={selectedAttributeIndex}.");
            }
            else
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

                var pickerTargets = targetsArray
                    .Select((t, i) =>
                    {
                        var label = t["label"]?.Value<string>() ?? $"Step definition {i + 1}";
                        var startLine = t["startLine"]?.Value<int>() ?? 0;
                        var startChar = t["startChar"]?.Value<int>() ?? 0;
                        // Build a display label that includes the source location
                        var displayText = $"{label}";
                        return new NavigationTarget(displayText, textView.Uri.LocalPath, startLine, startChar);
                    })
                    .ToList();

                var dialog = new NavigationPickerDialog("Choose step definition to rename", pickerTargets);
                if (dialog.ShowModal() != true || dialog.SelectedIndex < 0)
                {
                    _fileLogger.LogInfo("RenameStepCommand: picker dismissed.");
                    return;
                }

                selectedAttributeIndex = targetsArray[dialog.SelectedIndex]["attributeIndex"]?.Value<int>() ?? 0;
                _fileLogger.LogInfo($"RenameStepCommand: user selected target index={dialog.SelectedIndex}, attributeIndex={selectedAttributeIndex}.");
            }

            // ── Step 3: Tell the server which attribute was selected ────────
            // Use version 0 as a simple sentinel — the document version isn't critical here.
            await service.SelectRenameTargetAsync(fileUri, version: 0, selectedAttributeIndex, cancellationToken)
                .ConfigureAwait(false);

            // ── Step 4: Trigger VS native rename ────────────────────────────
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            var dte = ServiceProvider.GlobalProvider.GetService(typeof(EnvDTE.DTE)) as DTE2;
            if (dte is null)
            {
                _fileLogger.LogWarning("RenameStepCommand: DTE service not available.");
                VsUtils.ShowStatusBarMessage("Reqnroll: Press F2 to rename the selected step.");
                return;
            }

            // The RenameSessionManager (server-side) now has the pending session.
            // When the user triggers rename (or we execute Edit.Rename), the server
            // will match the pending session to the selected attribute.
            _fileLogger.LogInfo("RenameStepCommand: executing Edit.Rename via DTE.");
            dte.ExecuteCommand("Edit.Rename");

            _fileLogger.LogInfo("RenameStepCommand: Edit.Rename executed successfully.");
        }
        catch (Exception ex)
        {
            _fileLogger.LogWarning($"RenameStepCommand: failed: {ex}");
            _traceSource.TraceEvent(TraceEventType.Error, 0, "RenameStepCommand: failed: {0}", ex);
        }
    }
}
