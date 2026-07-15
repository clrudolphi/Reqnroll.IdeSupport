package com.reqnroll.ide.rider.codevision

import com.intellij.codeInsight.codeVision.CodeVisionAnchorKind
import com.intellij.codeInsight.codeVision.CodeVisionEntry
import com.intellij.codeInsight.codeVision.CodeVisionHost
import com.intellij.codeInsight.codeVision.CodeVisionProvider
import com.intellij.codeInsight.codeVision.CodeVisionRelativeOrdering
import com.intellij.codeInsight.codeVision.CodeVisionState
import com.intellij.codeInsight.codeVision.ui.model.ClickableTextCodeVisionEntry
import com.intellij.openapi.components.service
import com.intellij.openapi.editor.Editor
import com.intellij.openapi.editor.EditorFactory
import com.intellij.openapi.fileEditor.FileDocumentManager
import com.intellij.openapi.project.Project
import com.intellij.openapi.util.TextRange
import com.reqnroll.ide.rider.actions.FindStepUsagesRunner
import com.reqnroll.ide.rider.lsp.ReqnrollRequestSender
import com.intellij.util.io.URLUtil
import com.intellij.openapi.vfs.VirtualFileManager
import org.eclipse.lsp4j.CodeLens
import org.eclipse.lsp4j.Command

/**
 * "N step usages" CodeVision lens above each step-definition method in `.cs` files — the Rider
 * equivalent of VS Code's built-in CodeLens support for the standard `textDocument/codeLens`
 * request (see StepCodeLensHandler.cs). Rider's generic LSP client has no rendering-side
 * consumer for `textDocument/codeLens` at all (confirmed by decompiling — only capability-name
 * bookkeeping exists), so this calls the request directly via [ReqnrollRequestSender.codeLens]
 * and renders the results through IntelliJ's native CodeVision extension point instead.
 */
class StepUsagesCodeVisionProvider : CodeVisionProvider<Unit> {
    companion object {
        private const val ID = "Reqnroll.StepUsagesCodeVision"

        /**
         * Forces a recompute of this lens for every currently open `.cs` editor in [project] —
         * called from [com.reqnroll.ide.rider.lsp.ReqnrollCodeLensRefreshInterceptor] when the
         * server sends `workspace/codeLens/refresh`. Without this, IntelliJ's CodeVision engine
         * only recomputes on its own signals (document edits to the `.cs` file itself, editor
         * focus, etc.) — it has no way to know a `.feature` file's edit changed this file's step-
         * usage counts, so the lens silently goes stale (clicking still worked because that reruns
         * `findStepUsages` fresh; only the cached *count* was wrong).
         */
        fun refreshOpenCsEditors(project: Project) {
            val codeVisionHost = project.service<CodeVisionHost>()
            for (editor in EditorFactory.getInstance().allEditors) {
                if (editor.project != project) continue
                val virtualFile = FileDocumentManager.getInstance().getFile(editor.document) ?: continue
                if (!virtualFile.extension.equals("cs", ignoreCase = true)) continue
                codeVisionHost.invalidateProvider(CodeVisionHost.LensInvalidateSignal(editor, listOf(ID)))
            }
        }

        /** True if [lens] has a command and its start line actually exists in a [lineCount]-line document. Pure filtering logic, kept separate from live Editor/Document access so it's unit-testable. */
        internal fun isRenderable(lens: CodeLens, lineCount: Int): Boolean =
            lens.command != null && lens.range.start.line in 0 until lineCount

        /**
         * Builds the CodeVision entry rendered for [command], reporting as [providerId]. `internal`
         * (rather than folded into [computeEntries]) so the exact text/providerId wiring can be
         * regression-tested without a platform fixture — `ClickableTextCodeVisionEntry`'s
         * constructor is `(text, providerId, onClick, icon, longPresentation, tooltip,
         * extraActions)`, confirmed via the JVM parameter-name assertions embedded in its
         * decompiled bytecode; an earlier version of this code had `text`/`providerId` swapped, so
         * the lens displayed this provider's id ("Reqnroll.StepUsagesCodeVision") instead of the
         * actual usage count.
         */
        internal fun buildEntry(command: Command, providerId: String, onClick: () -> Unit): ClickableTextCodeVisionEntry =
            ClickableTextCodeVisionEntry(
                command.title, providerId, { _, _ -> onClick() },
                null, command.title, command.title, emptyList(),
            )
    }

    override val id: String = ID
    override val name: String = "Reqnroll step usages"
    override val relativeOrderings: List<CodeVisionRelativeOrdering> = emptyList()
    override val defaultAnchor: CodeVisionAnchorKind = CodeVisionAnchorKind.Default

    override fun precomputeOnUiThread(editor: Editor) = Unit

    override fun computeCodeVision(editor: Editor, uiData: Unit): CodeVisionState =
        CodeVisionState.Ready(computeEntries(editor))

    private fun computeEntries(editor: Editor): List<Pair<TextRange, CodeVisionEntry>> {
        val project = editor.project ?: return emptyList()
        val file = FileDocumentManager.getInstance().getFile(editor.document) ?: return emptyList()
        if (!file.extension.equals("cs", ignoreCase = true)) return emptyList()

        val uri = VirtualFileManager.constructUrl("file", URLUtil.encodePath(file.path))
        val lenses = ReqnrollRequestSender.codeLens(project, uri) ?: return emptyList()

        val document = editor.document
        return lenses.filter { isRenderable(it, document.lineCount) }.map { lens ->
            // isRenderable already confirmed lens.command is non-null; Kotlin can't smart-cast
            // across the filter/map boundary, hence the !!.
            val command = lens.command!!
            val line = lens.range.start.line
            val offset = document.getLineStartOffset(line)
            val entry = buildEntry(command, id) {
                if (command.command == "reqnroll.findStepUsages")
                    FindStepUsagesRunner.runAndShow(project, uri, line, 0)
                else
                    FindStepUsagesRunner.showNoUsages(project)
            }
            TextRange(offset, offset) to entry
        }
    }
}
