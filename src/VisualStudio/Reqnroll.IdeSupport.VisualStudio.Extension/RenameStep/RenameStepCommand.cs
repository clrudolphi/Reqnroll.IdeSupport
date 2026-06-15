#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.Extensibility.Commands;
using Microsoft.VisualStudio.Extensibility.Editor;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TextManager.Interop;
using Newtonsoft.Json.Linq;
using Reqnroll.IdeSupport.Common.Diagnostics;
using Reqnroll.IdeSupport.VisualStudio.Extension.Navigation;

namespace Reqnroll.IdeSupport.VisualStudio.Extension.RenameStep;

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
        VisibleWhen = ActivationConstraint.Or(
            ActivationConstraint.EditorContentType("CSharp"),
            ActivationConstraint.EditorContentType("reqnroll-gherkin")),
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
                VsUtils.ShowStatusBarMessage("Reqnroll: LSP server not yet initialized.");
                return;
            }

            var textView = await context.GetActiveTextViewAsync(cancellationToken).ConfigureAwait(false);
            if (textView is null)
            {
                _fileLogger.LogWarning("RenameStepCommand: No active text view in client context.");
                return;
            }

            var fileUri = textView.Uri.ToString();
            var caretPos = textView.Selection.ActivePosition;
            var line = caretPos.GetContainingLine();
            var lineNum = line.LineNumber;
            var charNum = caretPos.Offset - line.Text.Start;

            _fileLogger.LogInfo($"RenameStepCommand: active view uri='{fileUri}', caret line={lineNum} char={charNum}.");

            // Step 1: Get rename targets from the server
            var targets = await service.GetRenameTargetsAsync(fileUri, lineNum, charNum, cancellationToken)
                .ConfigureAwait(false);

            if (targets is null || targets.Targets.Count == 0)
            {
                _fileLogger.LogInfo("RenameStepCommand: no renameable targets at cursor position.");
                VsUtils.ShowStatusBarMessage("Reqnroll: No step definition found to rename at this position.");
                return;
            }

            // Step 2: Select target (picker if multiple)
            int selectedAttributeIndex;
            string currentLabel;
            string currentExpression;

            if (targets.Targets.Count == 1)
            {
                var item = targets.Targets[0];
                selectedAttributeIndex = item.AttributeIndex;
                currentLabel = item.Label;
                currentExpression = item.Expression;
                _fileLogger.LogInfo($"RenameStepCommand: single target, attributeIndex={selectedAttributeIndex}, label='{currentLabel}'.");
            }
            else
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

                var pickerTargets = targets.Targets
                    .Select(t => new NavigationTarget(t.Label, textView.Uri.LocalPath, 0, 0))
                    .ToList();

                var dialog = new NavigationPickerDialog("Choose step definition to rename", pickerTargets);
                if (dialog.ShowModal() != true || dialog.SelectedIndex < 0)
                {
                    _fileLogger.LogInfo("RenameStepCommand: picker dismissed.");
                    return;
                }

                var chosen = targets.Targets[dialog.SelectedIndex];
                selectedAttributeIndex = chosen.AttributeIndex;
                currentLabel = chosen.Label;
                currentExpression = chosen.Expression;
                _fileLogger.LogInfo($"RenameStepCommand: user selected target index={dialog.SelectedIndex}, attributeIndex={selectedAttributeIndex}.");
            }

            // Step 3: Tell the server which attribute was selected
            await service.SelectRenameTargetAsync(fileUri, version: 0, selectedAttributeIndex, cancellationToken)
                .ConfigureAwait(false);

            // Step 4: Prompt user for new step text
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            string currentStepText;
            if (!string.IsNullOrEmpty(currentExpression))
                currentStepText = currentExpression;
            else
            {
                var stepTypePrefix = currentLabel.IndexOf(' ') >= 0
                    ? currentLabel.Substring(0, currentLabel.IndexOf(' ')) + " "
                    : "";
                currentStepText = currentLabel.Length > stepTypePrefix.Length
                    ? currentLabel.Substring(stepTypePrefix.Length)
                    : currentLabel;
            }

            var newStepText = Microsoft.VisualBasic.Interaction.InputBox(
                "Enter the new step text:", "Rename Step", currentStepText);
            if (string.IsNullOrEmpty(newStepText))
            {
                _fileLogger.LogInfo("RenameStepCommand: user cancelled rename dialog.");
                return;
            }

            _fileLogger.LogInfo($"RenameStepCommand: user entered new text '{newStepText}'.");

            // Step 5: Send textDocument/rename via the service
            var result = await service.SendRenameRequestAsync(
                fileUri, lineNum, charNum, newStepText, cancellationToken)
                .ConfigureAwait(false);

            if (result is null)
            {
                _traceSource.TraceInformation("RenameStepCommand: server returned null from rename.");
                VsUtils.ShowStatusBarMessage("Reqnroll: Rename failed.");
                return;
            }

            _fileLogger.LogInfo($"RenameStepCommand: rename result = {result}");

            // Step 6: Apply the WorkspaceEdit
            await ApplyWorkspaceEditAsync(result, cancellationToken).ConfigureAwait(false);

            _fileLogger.LogInfo("RenameStepCommand: rename completed successfully.");
        }
        catch (Exception ex)
        {
            _fileLogger.LogWarning($"RenameStepCommand: failed: {ex}");
            _traceSource.TraceEvent(TraceEventType.Error, 0, "RenameStepCommand: failed: {0}", ex);
        }
    }

    // ── Workspace edit application ───────────────────────────────────────────

    private async Task ApplyWorkspaceEditAsync(JToken result, CancellationToken cancellationToken)
    {
        if (result is not JObject editObj)
        {
            _fileLogger.LogWarning("RenameStepCommand: rename result is not a JSON object.");
            return;
        }

        var changes = editObj["changes"] as JObject;
        if (changes is null)
        {
            _fileLogger.LogWarning("RenameStepCommand: rename result has no 'changes' property.");
            return;
        }

        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        // Use the Running Document Table to find open documents.
        // For files open in the editor, apply edits via VS text buffer so unsaved
        // changes are not overwritten by File.WriteAllText.
        var rdt = ServiceProvider.GlobalProvider.GetService(typeof(SVsRunningDocumentTable))
            as IVsRunningDocumentTable;

        foreach (var fileEntry in changes)
        {
            var uri = fileEntry.Key;
            var edits = fileEntry.Value as JArray;
            if (edits is null || edits.Count == 0)
                continue;

            // Convert file URI to local path
            var localPath = uri;
            if (localPath.StartsWith("file:///", StringComparison.OrdinalIgnoreCase))
                localPath = localPath.Substring(8).Replace('/', '\\');

            _fileLogger.LogInfo($"RenameStepCommand: applying {edits.Count} edit(s) to '{localPath}'.");

            if (!System.IO.File.Exists(localPath))
            {
                _fileLogger.LogWarning($"RenameStepCommand: file not found: '{localPath}'.");
                continue;
            }

            // Parse edits into a sorted list (bottom-to-top so positions stay valid)
            var textEdits = new List<(int startLine, int startChar, int endLine, int endChar, string newText)>();
            foreach (var edit in edits.Cast<JObject>())
            {
                var range = edit["range"];
                if (range is null) continue;
                var start = range["start"];
                var end = range["end"];
                if (start is null || end is null) continue;

                textEdits.Add((
                    start["line"]?.Value<int>() ?? 0,
                    start["character"]?.Value<int>() ?? 0,
                    end["line"]?.Value<int>() ?? 0,
                    end["character"]?.Value<int>() ?? 0,
                    edit["newText"]?.Value<string>() ?? ""
                ));
            }

            textEdits.Sort((a, b) =>
            {
                var lineCmp = b.startLine.CompareTo(a.startLine);
                return lineCmp != 0 ? lineCmp : b.startChar.CompareTo(a.startChar);
            });

            // Try to apply via VS text buffer when the document is open
            bool appliedToBuffer = false;
            if (rdt != null)
            {
                var hr = rdt.FindAndLockDocument(
                    1, // RDT_NoLock
                    localPath,
                    out _,
                    out _,
                    out var docDataPtr,
                    out _);
                if (hr == 0 && docDataPtr != IntPtr.Zero)
                {
                    try
                    {
                        var docObj = Marshal.GetObjectForIUnknown(docDataPtr);
                        if (docObj is IVsTextLines textLines)
                        {
                            foreach (var (sl, sc, el, ec, nt) in textEdits)
                            {
                                var pszText = Marshal.StringToCoTaskMemUni(nt);
                                try
                                {
                                    var editHr = textLines.ReplaceLines(
                                        sl, sc, el, ec, pszText, nt.Length, null);
                                    if (editHr != 0)
                                        _fileLogger.LogWarning(
                                            $"RenameStepCommand: ReplaceLines failed (hr=0x{editHr:X8}) at ({sl},{sc})-({el},{ec})");
                                }
                                finally
                                {
                                    Marshal.FreeCoTaskMem(pszText);
                                }
                            }

                            appliedToBuffer = true;
                            _fileLogger.LogInfo($"RenameStepCommand: applied edits via text buffer for '{localPath}'.");

                            // Send didChange for .feature files
                            if (localPath.EndsWith(".feature", StringComparison.OrdinalIgnoreCase))
                            {
                                var bufferText = ReadBufferText(textLines);
                                if (bufferText is not null)
                                {
                                    _ = _state.Service!.SendDidChangeAsync(localPath, bufferText, cancellationToken);
                                    _fileLogger.LogInfo($"RenameStepCommand: sent didChange for '{localPath}'.");
                                }
                            }
                        }
                    }
                    finally
                    {
                        Marshal.Release(docDataPtr);
                    }
                }
            }

            if (appliedToBuffer)
                continue;

            // Fall back to File.WriteAllText for closed or non-open documents
            var fileText = System.IO.File.ReadAllText(localPath);
            var fileLines = fileText.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

            foreach (var (sl, sc, el, ec, nt) in textEdits)
            {
                if (sl >= fileLines.Length) continue;

                var currentLine = fileLines[sl];
                var newLine = currentLine.Substring(0, sc) + nt;
                if (el < fileLines.Length)
                {
                    var endLine = fileLines[el];
                    if (ec <= endLine.Length)
                        newLine += endLine.Substring(ec);
                }
                fileLines[sl] = newLine;

                for (int i = sl + 1; i <= el && i < fileLines.Length; i++)
                    fileLines[i] = null!;
            }

            var finalLines = fileLines.Where(l => l != null).ToArray();
            var newContent = string.Join(Environment.NewLine, finalLines);
            System.IO.File.WriteAllText(localPath, newContent);

            _fileLogger.LogInfo($"RenameStepCommand: wrote {finalLines.Length} lines to '{localPath}'.");

            // Notify the LSP server about the change to a .feature file
            if (localPath.EndsWith(".feature", StringComparison.OrdinalIgnoreCase))
            {
                _ = _state.Service!.SendDidChangeAsync(localPath, newContent, cancellationToken);
                _fileLogger.LogInfo($"RenameStepCommand: sent didChange for '{localPath}'.");
            }
        }

        VsUtils.ShowStatusBarMessage("Reqnroll: Step renamed successfully.");
    }

    /// <summary>
    /// Reads the full text content of an <see cref="IVsTextLines"/> buffer.
    /// </summary>
    private static string? ReadBufferText(IVsTextLines textLines)
    {
        try
        {
            var hr = textLines.GetLineCount(out var lineCount);
            if (hr != 0 || lineCount <= 0) return null;

            var sb = new StringBuilder();
            for (int i = 0; i < lineCount; i++)
            {
                hr = textLines.GetLengthOfLine(i, out var length);
                if (hr != 0) continue;

                hr = textLines.GetLineText(i, 0, i, length, out var lineText);
                if (hr == 0 && lineText is not null)
                {
                    sb.Append(lineText);
                    if (i < lineCount - 1)
                        sb.Append('\n');
                }
            }
            return sb.ToString();
        }
        catch
        {
            return null;
        }
    }
}
