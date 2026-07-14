package com.reqnroll.ide.rider.inlayhints

import com.intellij.codeInsight.hints.declarative.HintFormat
import com.intellij.codeInsight.hints.declarative.InlayHintsCollector
import com.intellij.codeInsight.hints.declarative.InlayHintsProvider
import com.intellij.codeInsight.hints.declarative.InlayTreeSink
import com.intellij.codeInsight.hints.declarative.InlineInlayPosition
import com.intellij.codeInsight.hints.declarative.OwnBypassCollector
import com.intellij.openapi.editor.Editor
import com.intellij.openapi.fileEditor.FileDocumentManager
import com.intellij.psi.PsiDocumentManager
import com.intellij.psi.PsiFile
import com.intellij.util.io.URLUtil
import com.intellij.openapi.vfs.VirtualFileManager
import com.reqnroll.ide.rider.lsp.ReqnrollRequestSender

/**
 * Binding-info inlay hints for `.feature` files (F23 on the server — see
 * FeatureInlayHintHandler.cs) — shows the bound step definition's method name at the end of
 * each step line. Rider's generic LSP client has no rendering-side consumer for
 * `textDocument/inlayHint` at all (confirmed by decompiling — same bookkeeping-only status as
 * `textDocument/codeLens`), so this calls the request directly via
 * [ReqnrollRequestSender.inlayHint] and renders through IntelliJ's native declarative inlay
 * hints extension point (`com.intellij.codeInsight.declarativeInlayProvider`) instead.
 */
class FeatureStepInlayHintsProvider : InlayHintsProvider {
    override fun createCollector(file: PsiFile, editor: Editor): InlayHintsCollector =
        Collector

    private object Collector : OwnBypassCollector {
        override fun collectHintsForFile(file: PsiFile, sink: InlayTreeSink) {
            val project = file.project
            val virtualFile = file.virtualFile ?: return
            val document = PsiDocumentManager.getInstance(project).getDocument(file) ?: return

            val uri = VirtualFileManager.constructUrl("file", URLUtil.encodePath(virtualFile.path))
            val hints = ReqnrollRequestSender.inlayHint(project, uri, 0, document.lineCount) ?: return

            for (hint in hints) {
                val line = hint.position.line
                if (line !in 0 until document.lineCount) continue
                val lineEndOffset = document.getLineEndOffset(line)
                val character = hint.position.character.coerceAtMost(lineEndOffset - document.getLineStartOffset(line))
                val offset = document.getLineStartOffset(line) + character

                val label = hint.label.left ?: continue
                sink.addPresentation(
                    InlineInlayPosition(offset, true, 0),
                    emptyList(),
                    hint.tooltip?.left,
                    HintFormat.default,
                ) {
                    text(label)
                }
            }
        }
    }
}
