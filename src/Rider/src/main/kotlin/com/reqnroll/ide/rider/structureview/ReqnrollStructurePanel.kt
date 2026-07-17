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
import javax.swing.JPanel
import javax.swing.SwingConstants

/**
 * Swaps between a live [StructureView] (rebuilt from [ReqnrollFeatureStructureViewBuilder] for the
 * currently selected `.feature` file) and a placeholder label for every other file. Tracks the
 * active editor itself via [FileEditorManagerListener] — there's no PSI/FileType routing into this
 * tool window the way IntelliJ's built-in "Structure" tool window gets for languages with a
 * `ParserDefinition`, since this tool window is entirely our own (see
 * [ReqnrollStructureToolWindowFactory]'s doc comment for why).
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

        val selectedEditor = FileEditorManager.getInstance(project).selectedEditor
        refresh(selectedEditor?.file, selectedEditor)
    }

    override fun dispose() {
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
    }
}
