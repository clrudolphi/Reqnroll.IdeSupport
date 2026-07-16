package com.reqnroll.ide.rider.structureview

import com.intellij.ide.structureView.StructureViewModelBase
import com.intellij.ide.structureView.StructureViewTreeElement
import com.intellij.ide.util.treeView.smartTree.TreeElement
import com.intellij.navigation.ItemPresentation
import com.intellij.openapi.application.ApplicationManager
import com.intellij.openapi.application.ModalityState
import com.intellij.openapi.editor.Editor
import com.intellij.openapi.editor.event.DocumentEvent
import com.intellij.openapi.editor.event.DocumentListener
import com.intellij.openapi.fileEditor.OpenFileDescriptor
import com.intellij.openapi.project.Project
import com.intellij.openapi.util.Disposer
import com.intellij.openapi.vfs.VirtualFile
import com.intellij.openapi.vfs.VirtualFileManager
import com.intellij.psi.PsiFile
import com.intellij.util.Alarm
import com.intellij.util.io.URLUtil
import com.reqnroll.ide.rider.logging.ReqnrollDebugLogger
import com.reqnroll.ide.rider.lsp.ReqnrollRequestSender
import org.eclipse.lsp4j.DocumentSymbol

/**
 * Feeds Structure View from `textDocument/documentSymbol`. Unlike a PSI-based
 * `StructureViewModel`, there's no PSI-change signal to auto-refresh from (`.feature` files have
 * no `ParserDefinition` — see [ReqnrollFeatureStructureViewFactory]), so this drives its own
 * debounced-edit refresh, mirroring
 * [com.reqnroll.ide.rider.folding.ReqnrollFeatureFoldingController]'s `Alarm`/`DocumentListener`
 * pattern exactly (same 400ms debounce, same background-request-then-`invokeLater` shape).
 *
 * [getRoot] is overridden rather than relying on [StructureViewModelBase]'s fixed stored root
 * field (confirmed via `javap` that the getter isn't `final`), since the tree needs to reflect
 * the latest `documentSymbol` response, not just the placeholder passed to the constructor before
 * the first request completes.
 */
class ReqnrollFeatureStructureViewModel(
    private val project: Project,
    private val virtualFile: VirtualFile,
    psiFile: PsiFile,
    editor: Editor?,
) : StructureViewModelBase(psiFile, FeatureFileRootElement(project, virtualFile, emptyList())) {
    @Volatile
    private var symbols: List<DocumentSymbol> = emptyList()

    private val disposable = Disposer.newDisposable("Reqnroll.FeatureStructureView:${virtualFile.path}")
    private val alarm = Alarm(Alarm.ThreadToUse.SWING_THREAD, disposable)

    init {
        // No live-refresh-on-edit without an editor to listen to (e.g. the "File Structure"
        // popup invoked without an open editor) — the model still loads once, just doesn't track
        // subsequent edits. Structure View toolwindow usage always supplies a real editor.
        editor?.document?.addDocumentListener(
            object : DocumentListener {
                override fun documentChanged(event: DocumentEvent) {
                    alarm.cancelAllRequests()
                    if (!alarm.isDisposed) alarm.addRequest({ refresh() }, DEBOUNCE_MS)
                }
            },
            disposable,
        )
        refresh()
    }

    override fun getRoot(): StructureViewTreeElement = FeatureFileRootElement(project, virtualFile, symbols)

    override fun dispose() {
        Disposer.dispose(disposable)
        super.dispose()
    }

    private fun refresh() {
        if (project.isDisposed) return

        ApplicationManager.getApplication().executeOnPooledThread {
            if (project.isDisposed) return@executeOnPooledThread

            val uri = VirtualFileManager.constructUrl("file", URLUtil.encodePath(virtualFile.path))
            val result = ReqnrollRequestSender.documentSymbol(project, uri)
            ReqnrollDebugLogger.info("ReqnrollFeatureStructureViewModel: ${result?.size ?: "null"} top-level symbol(s) for $uri")
            if (result == null) return@executeOnPooledThread

            symbols = result
            ApplicationManager.getApplication().invokeLater(
                {
                    if (!project.isDisposed) fireModelUpdate()
                },
                ModalityState.any(),
            )
        }
    }

    companion object {
        private const val DEBOUNCE_MS = 400
    }
}

/**
 * Synthetic whole-file root required by [StructureViewModelBase]'s constructor — hidden from the
 * tree via [ReqnrollFeatureStructureViewBuilder.isRootNodeShown], so its own presentation is
 * never actually shown; its [getChildren] (the top-level `documentSymbol` results, normally just
 * the single `Feature` symbol) are what render.
 */
private class FeatureFileRootElement(
    private val project: Project,
    private val virtualFile: VirtualFile,
    private val topLevelSymbols: List<DocumentSymbol>,
) : StructureViewTreeElement {
    override fun getValue(): Any = virtualFile

    override fun getPresentation(): ItemPresentation = object : ItemPresentation {
        override fun getPresentableText(): String = virtualFile.name
        override fun getIcon(unused: Boolean) = null
    }

    override fun getChildren(): Array<TreeElement> =
        topLevelSymbols.map { ReqnrollSymbolTreeElement(project, virtualFile, it) }.toTypedArray()

    override fun navigate(requestFocus: Boolean) {
        OpenFileDescriptor(project, virtualFile, 0).navigate(requestFocus)
    }

    override fun canNavigate(): Boolean = true

    override fun canNavigateToSource(): Boolean = true
}
