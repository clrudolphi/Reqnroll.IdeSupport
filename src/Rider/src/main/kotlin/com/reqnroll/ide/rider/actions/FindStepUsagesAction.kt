package com.reqnroll.ide.rider.actions

import com.intellij.openapi.actionSystem.ActionUpdateThread
import com.intellij.openapi.actionSystem.AnAction
import com.intellij.openapi.actionSystem.AnActionEvent
import com.intellij.openapi.actionSystem.CommonDataKeys
import com.intellij.openapi.application.ApplicationManager
import com.intellij.openapi.progress.ProgressIndicator
import com.intellij.openapi.progress.ProgressManager
import com.intellij.openapi.progress.Task
import com.intellij.openapi.project.Project
import com.intellij.openapi.ui.Messages
import com.intellij.openapi.vfs.VirtualFileManager
import com.intellij.util.io.URLUtil
import com.reqnroll.ide.rider.lsp.ReqnrollRequestSender
import com.reqnroll.ide.rider.lsp.protocol.FindStepUsageItem
import com.reqnroll.ide.rider.lsp.protocol.FindStepUsagesResponse

/**
 * Find Step Definition Usages — the Rider-side surface for the position-based
 * `reqnroll/findStepUsages` request. Mirrors VS Code's `doFindStepUsages`
 * (src/VSCode/src/stepUsages.ts): only enabled with the caret in a `.cs` file (matching VS
 * Code's `editorLangId == csharp` context-menu condition), sends the current caret position,
 * and shows a chooser popup of matching feature-file steps, navigating on selection.
 */
class FindStepUsagesAction : AnAction() {
    override fun getActionUpdateThread(): ActionUpdateThread = ActionUpdateThread.BGT

    override fun update(e: AnActionEvent) {
        val file = e.getData(CommonDataKeys.VIRTUAL_FILE)
        e.presentation.isEnabledAndVisible =
            e.project != null && e.getData(CommonDataKeys.EDITOR) != null &&
                file != null && file.extension.equals("cs", ignoreCase = true)
    }

    override fun actionPerformed(e: AnActionEvent) {
        val project = e.project ?: return
        val editor = e.getData(CommonDataKeys.EDITOR) ?: return
        val file = e.getData(CommonDataKeys.VIRTUAL_FILE) ?: return

        val uri = VirtualFileManager.constructUrl("file", URLUtil.encodePath(file.path))
        val position = editor.caretModel.logicalPosition

        ProgressManager.getInstance().run(object : Task.Backgroundable(
            project, "Reqnroll: Finding Step Usages", true) {
            override fun run(indicator: ProgressIndicator) {
                val response = ReqnrollRequestSender.findStepUsages(project, uri, position.line, position.column)
                ApplicationManager.getApplication().invokeLater { showResult(project, response) }
            }
        })
    }

    private fun showResult(project: Project, response: FindStepUsagesResponse?) {
        if (response == null) {
            Messages.showErrorDialog(
                project, "The Reqnroll LSP server is not running or did not respond.", "Find Step Usages")
            return
        }

        if (!response.isBinding) {
            Messages.showInfoMessage(
                project, "The caret is not on a step definition binding.", "Find Step Usages")
            return
        }

        if (response.locations.isEmpty()) {
            Messages.showInfoMessage(
                project, "This step definition has no usages in any feature file.", "Find Step Usages")
            return
        }

        ReqnrollResultPopup.show(
            project,
            "${response.locations.size} Step Usage(s)",
            response.locations,
            render = { item -> renderLabel(item) },
            onChosen = { item -> ReqnrollResultPopup.navigateToUri(project, item.uri, item.startLine, item.startChar) },
        )
    }

    private fun renderLabel(item: FindStepUsageItem): String {
        val keyword = item.keyword?.let { "$it " } ?: ""
        val text = item.stepText ?: item.uri.substringAfterLast('/')
        val scenario = item.scenarioName?.let { " — $it" } ?: ""
        val project = item.projectName?.let { " [$it]" } ?: ""
        return "$keyword$text$scenario$project"
    }
}
