package com.reqnroll.ide.rider.inlayhints

import com.intellij.openapi.application.ApplicationManager
import com.intellij.openapi.application.ModalityState
import com.intellij.openapi.editor.Document
import com.intellij.openapi.editor.Editor
import com.intellij.openapi.editor.Inlay
import com.intellij.openapi.editor.colors.EditorFontType
import com.intellij.openapi.editor.event.DocumentEvent
import com.intellij.openapi.editor.event.DocumentListener
import com.intellij.openapi.editor.event.EditorFactoryEvent
import com.intellij.openapi.editor.event.EditorFactoryListener
import com.intellij.openapi.editor.markup.TextAttributes
import com.intellij.openapi.fileEditor.FileDocumentManager
import com.intellij.openapi.util.Disposer
import com.intellij.openapi.util.Key
import com.intellij.openapi.vfs.VirtualFile
import com.intellij.openapi.vfs.VirtualFileManager
import com.intellij.ui.JBColor
import com.intellij.util.Alarm
import com.intellij.util.io.URLUtil
import com.reqnroll.ide.rider.logging.ReqnrollDebugLogger
import com.reqnroll.ide.rider.lsp.ReqnrollRequestSender
import org.eclipse.lsp4j.InlayHint
import java.awt.Graphics
import java.awt.Rectangle

/**
 * Renders binding-info inlay hints for `.feature` files directly against
 * [Editor.getInlayModel], bypassing IntelliJ's declarative inlay-hints framework entirely.
 *
 * That framework dispatches by PSI language (a [com.intellij.codeInsight.hints.declarative.InlayHintsProvider]
 * registers for one via `language=` in `plugin.xml`), but `.feature` files have no
 * [com.intellij.lang.ParserDefinition] registered for
 * [com.reqnroll.ide.rider.ReqnrollFeatureLanguage] — confirmed via decompiling
 * `PsiPlainTextFileImpl`/`PlainTextParserDefinition` that IntelliJ's built-in fallback for a
 * language with no parser hardcodes `PlainTextLanguage`, not the file's declared language. So a
 * declarative provider registered for "Reqnroll Feature" was silently never invoked — confirmed
 * live: zero `FeatureInlayHintHandler` requests ever reached the server, with no error anywhere.
 * Managing inlays here instead matches how every other `.feature` feature in this plugin already
 * works (semantic tokens, diagnostics, CodeLens): driven directly off the LSP request, keyed by
 * URI/[Editor], with no PSI dependency at all.
 *
 * Refresh is client-side polling, not server push: there's no `workspace/inlayHint/refresh`
 * wiring (client or server) in this pipeline, so hints are recomputed on document edits and on a
 * few bounded delays after the editor opens, to catch up once the async project/binding-discovery
 * pipeline (see `ReqnrollLspServerReadiness`) finishes shortly after open.
 */
class ReqnrollFeatureInlayHintsController : EditorFactoryListener {
    private class Session(val disposable: com.intellij.openapi.Disposable, val alarm: Alarm, val docListener: DocumentListener)

    override fun editorCreated(event: EditorFactoryEvent) {
        val editor = event.editor
        val project = editor.project ?: return
        val virtualFile = FileDocumentManager.getInstance().getFile(editor.document) ?: return
        if (virtualFile.extension != "feature") return

        val disposable = Disposer.newDisposable("ReqnrollFeatureInlayHints:${virtualFile.path}")
        val alarm = Alarm(Alarm.ThreadToUse.SWING_THREAD, disposable)

        fun scheduleRefresh(delayMs: Int) {
            if (!alarm.isDisposed) alarm.addRequest({ refresh(project, editor, virtualFile) }, delayMs)
        }

        val docListener = object : DocumentListener {
            override fun documentChanged(event: DocumentEvent) {
                alarm.cancelAllRequests()
                scheduleRefresh(DEBOUNCE_MS)
            }
        }
        editor.document.addDocumentListener(docListener, disposable)
        editor.putUserData(SESSION_KEY, Session(disposable, alarm, docListener))

        // Initial paint, plus bounded catch-up retries for the async binding-discovery window
        // right after the server/project just started (see class doc).
        scheduleRefresh(200)
        scheduleRefresh(1500)
        scheduleRefresh(4000)
    }

    override fun editorReleased(event: EditorFactoryEvent) {
        val editor = event.editor
        val session = editor.getUserData(SESSION_KEY) ?: return
        editor.putUserData(SESSION_KEY, null)
        Disposer.dispose(session.disposable)
        clearInlays(editor)
    }

    private fun refresh(project: com.intellij.openapi.project.Project, editor: Editor, virtualFile: VirtualFile) {
        if (project.isDisposed || editor.isDisposed) return

        val uri = VirtualFileManager.constructUrl("file", URLUtil.encodePath(virtualFile.path))
        val hints = ReqnrollRequestSender.inlayHint(project, uri, 0, editor.document.lineCount)
        ReqnrollDebugLogger.info("ReqnrollFeatureInlayHintsController: ${hints?.size ?: "null"} hint(s) for $uri")

        ApplicationManager.getApplication().invokeLater(
            {
                if (!editor.isDisposed) renderInlays(editor, editor.document, hints.orEmpty())
            },
            ModalityState.any(),
        )
    }

    private fun renderInlays(editor: Editor, document: Document, hints: List<InlayHint>) {
        clearInlays(editor)
        val inlays = mutableListOf<Inlay<*>>()

        for (hint in hints) {
            val line = hint.position.line
            if (line !in 0 until document.lineCount) continue
            val lineEndOffset = document.getLineEndOffset(line)
            val character = hint.position.character.coerceAtMost(lineEndOffset - document.getLineStartOffset(line))
            val offset = document.getLineStartOffset(line) + character
            val label = hint.label.left ?: continue

            val inlay = editor.inlayModel.addInlineElement(offset, true, BindingHintRenderer(label)) ?: continue
            inlays += inlay
        }

        editor.putUserData(INLAYS_KEY, inlays)
    }

    private fun clearInlays(editor: Editor) {
        editor.getUserData(INLAYS_KEY)?.forEach { Disposer.dispose(it) }
        editor.putUserData(INLAYS_KEY, null)
    }

    /** Paints a single-line, greyed-out text label — no tooltip yet (see class doc: v1 scope). */
    private class BindingHintRenderer(private val text: String) : com.intellij.openapi.editor.EditorCustomElementRenderer {
        override fun calcWidthInPixels(inlay: Inlay<*>): Int {
            val editor = inlay.editor
            val metrics = editor.contentComponent.getFontMetrics(editor.colorsScheme.getFont(EditorFontType.PLAIN))
            return metrics.stringWidth(text) + 2 * PADDING_PX
        }

        override fun paint(inlay: Inlay<*>, g: Graphics, targetRegion: Rectangle, textAttributes: TextAttributes) {
            val editor = inlay.editor
            g.font = editor.colorsScheme.getFont(EditorFontType.PLAIN)
            g.color = JBColor.GRAY
            val ascent = g.fontMetrics.ascent
            g.drawString(text, targetRegion.x + PADDING_PX, targetRegion.y + ascent)
        }
    }

    private companion object {
        const val DEBOUNCE_MS = 400
        const val PADDING_PX = 4
        val SESSION_KEY = Key.create<Session>("Reqnroll.FeatureInlayHints.Session")
        val INLAYS_KEY = Key.create<MutableList<Inlay<*>>>("Reqnroll.FeatureInlayHints.Inlays")
    }
}
