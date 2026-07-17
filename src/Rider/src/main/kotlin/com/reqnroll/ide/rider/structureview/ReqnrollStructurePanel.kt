package com.reqnroll.ide.rider.structureview

import com.intellij.ide.structureView.StructureView
import com.intellij.openapi.Disposable
import com.intellij.openapi.fileEditor.FileEditor
import com.intellij.openapi.fileEditor.FileEditorManager
import com.intellij.openapi.fileEditor.FileEditorManagerEvent
import com.intellij.openapi.fileEditor.FileEditorManagerListener
import com.intellij.openapi.project.Project
import com.intellij.openapi.util.Disposer
import com.intellij.openapi.vfs.VirtualFile
import com.intellij.ui.components.JBLabel
import com.intellij.util.ui.JBUI
import java.awt.CardLayout
import java.util.concurrent.ConcurrentHashMap
import javax.swing.JPanel
import javax.swing.SwingConstants

/**
 * Swaps between a live [StructureView] (rebuilt from [ReqnrollFeatureStructureViewBuilder] for the
 * currently selected `.feature` file) and a placeholder label for every other file. Tracks the
 * active editor itself via [FileEditorManagerListener] — there's no PSI/FileType routing into this
 * tool window the way IntelliJ's built-in "Structure" tool window gets for languages with a
 * `ParserDefinition`, since this tool window is entirely our own (see
 * [ReqnrollStructureToolWindowFactory]'s doc comment for why).
 *
 * [refreshActivePanel] exists for the same reason
 * [com.reqnroll.ide.rider.folding.ReqnrollFeatureFoldingController.refreshOpenFeatureEditors]
 * does: if the tool window is shown (e.g. restored from a previous session, with a `.feature` file
 * already the active tab) before the LSP server is up, the very first [refresh] gets nothing back
 * and there's otherwise no trigger to retry it — confirmed live (issue #163 follow-up), the panel
 * stayed empty until the user switched tabs away and back, which just happens to force a fresh
 * [refresh] via [selectionChanged][FileEditorManagerListener.selectionChanged]. Wired into
 * [com.reqnroll.ide.rider.lsp.ReqnrollInlayHintRefreshInterceptor] alongside its other
 * once-the-server-is-actually-up refreshes.
 */
class ReqnrollStructurePanel(private val project: Project) : Disposable {
    private val placeholder = JBLabel("Open a .feature file to view its structure", SwingConstants.CENTER).apply {
        border = JBUI.Borders.empty(8)
    }
    private val cards = JPanel(CardLayout()).apply {
        add(placeholder, PLACEHOLDER_CARD)
    }

    val component: JPanel get() = cards

    private var activeStructureView: StructureView? = null

    init {
        project.messageBus.connect(this).subscribe(
            FileEditorManagerListener.FILE_EDITOR_MANAGER,
            object : FileEditorManagerListener {
                override fun selectionChanged(event: FileEditorManagerEvent) {
                    refresh(event.newFile, event.newEditor)
                }
            },
        )

        activePanels[project] = this
        val selectedEditor = FileEditorManager.getInstance(project).selectedEditor
        refresh(selectedEditor?.file, selectedEditor)
    }

    override fun dispose() {
        activePanels.remove(project, this)
        clearStructureView()
    }

    private fun refresh(virtualFile: VirtualFile?, fileEditor: FileEditor?) {
        if (project.isDisposed) return

        if (virtualFile == null || fileEditor == null || !virtualFile.extension.equals("feature", ignoreCase = true)) {
            clearStructureView()
            (cards.layout as CardLayout).show(cards, PLACEHOLDER_CARD)
            return
        }

        clearStructureView()
        val structureView = ReqnrollFeatureStructureViewBuilder(project, virtualFile).createStructureView(fileEditor, project)
        activeStructureView = structureView
        Disposer.register(this, structureView)

        cards.add(structureView.component, STRUCTURE_CARD)
        (cards.layout as CardLayout).show(cards, STRUCTURE_CARD)
    }

    private fun clearStructureView() {
        val structureView = activeStructureView ?: return
        activeStructureView = null
        cards.remove(structureView.component)
        Disposer.dispose(structureView)
    }

    companion object {
        private const val PLACEHOLDER_CARD = "placeholder"
        private const val STRUCTURE_CARD = "structure"
        private val activePanels = ConcurrentHashMap<Project, ReqnrollStructurePanel>()

        /** Re-triggers the currently active `.feature` file's Structure View fetch, if the tool window is currently shown for [project]. No-op otherwise. */
        fun refreshActivePanel(project: Project) {
            val panel = activePanels[project] ?: return
            val selectedEditor = FileEditorManager.getInstance(project).selectedEditor
            panel.refresh(selectedEditor?.file, selectedEditor)
        }
    }
}
