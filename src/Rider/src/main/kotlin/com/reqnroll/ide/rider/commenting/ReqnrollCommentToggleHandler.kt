package com.reqnroll.ide.rider.commenting

import com.intellij.openapi.actionSystem.DataContext
import com.intellij.openapi.application.ApplicationManager
import com.intellij.openapi.editor.Caret
import com.intellij.openapi.editor.Editor
import com.intellij.openapi.editor.actionSystem.EditorActionHandler
import com.intellij.openapi.fileEditor.FileDocumentManager
import com.intellij.openapi.vfs.VirtualFileManager
import com.intellij.util.io.URLUtil
import com.reqnroll.ide.rider.lsp.ReqnrollRequestSender

/**
 * Wraps IntelliJ's built-in "Comment with Line Comment" (`IdeActions.ACTION_COMMENT_LINE`)
 * handler so it also works on `.feature` files (issue #159). Neither VS nor VS Code has a native
 * `#`-comment understanding of Gherkin either — both redirect their own comment-toggle shortcut
 * to the server's `workspace/executeCommand` (`reqnroll.toggleComment`), which replies with a
 * `workspace/applyEdit` the client's built-in LSP infrastructure applies natively. Rider has no
 * `LanguageCommenter` registered for `.feature` (no PSI language, same recurring reason as inlay
 * hints/folding), so the built-in action is otherwise a no-op there — this wrapper delegates to
 * [original] for every other file type and only intercepts `.feature` files.
 *
 * Overrides the `protected` `isEnabledForCaret`/`doExecute` extension points, not the `public`
 * `isEnabled`/`execute` entry points those delegate to for the current caret — the latter are
 * plain (non-abstract, non-open-by-convention) convenience wrappers, not the class's actual
 * override contract. [runForAllCarets] is forced to `false` so `doExecute` fires once per
 * invocation (against the primary caret) rather than once per multi-caret, since the server
 * command only accepts a single line range — matching VS/VS Code, which likewise only look at the
 * single active selection.
 *
 * Installed once, application-wide, by [ReqnrollCommentToggleInstaller] via
 * `EditorActionManager.setActionHandler` — there is no declarative `plugin.xml` extension point
 * for "decorate an existing editor action handler."
 */
class ReqnrollCommentToggleHandler(private val original: EditorActionHandler) : EditorActionHandler() {
    override fun runForAllCarets(): Boolean = false

    override fun isEnabledForCaret(editor: Editor, caret: Caret, dataContext: DataContext): Boolean {
        if (isFeatureFile(editor)) return true
        return original.isEnabled(editor, caret, dataContext)
    }

    override fun doExecute(editor: Editor, caret: Caret?, dataContext: DataContext) {
        if (!isFeatureFile(editor)) {
            original.execute(editor, caret ?: editor.caretModel.currentCaret, dataContext)
            return
        }

        val project = editor.project ?: return
        val virtualFile = FileDocumentManager.getInstance().getFile(editor.document) ?: return
        val (startLine, endLine) = selectionLines(editor, caret ?: editor.caretModel.currentCaret)

        val uri = VirtualFileManager.constructUrl("file", URLUtil.encodePath(virtualFile.path))

        // ReqnrollRequestSender.toggleComment uses sendRequestSync, which blocks the calling
        // thread — doExecute() runs on the EDT (it's an editor-action handler), so the request
        // itself must run on a background thread, matching every other Reqnroll request-sender
        // caller's identical rationale. The actual edit arrives asynchronously via the server's
        // own workspace/applyEdit request, applied natively by Rider's platform Lsp4jClient — this
        // handler has nothing further to do once the command is dispatched.
        ApplicationManager.getApplication().executeOnPooledThread {
            ReqnrollRequestSender.toggleComment(project, uri, startLine, endLine)
        }
    }

    companion object {
        private fun isFeatureFile(editor: Editor): Boolean {
            val virtualFile = FileDocumentManager.getInstance().getFile(editor.document) ?: return false
            return virtualFile.extension.equals("feature", ignoreCase = true)
        }

        private fun selectionLines(editor: Editor, caret: Caret): Pair<Int, Int> {
            val document = editor.document
            if (!caret.hasSelection()) {
                val line = document.getLineNumber(caret.offset)
                return line to line
            }

            val startLine = document.getLineNumber(caret.selectionStart)
            val endOffset = caret.selectionEnd
            val endLineRaw = document.getLineNumber(endOffset)
            val endChar = endOffset - document.getLineStartOffset(endLineRaw)
            return normalizeSelectionLines(startLine, endLineRaw, endChar)
        }

        /**
         * Mirrors VS Code's `normalizeSelectionLines` (`selectionUtils.ts`): a selection dragged
         * down to column 0 of a line without selecting any character on it (common when
         * extending a selection with Shift+Down) reports that line as the end line, but the user
         * didn't mean to include it — drop it back to the previous line. `internal` (rather than
         * private) purely so it's unit-testable without a real [Editor]/[com.intellij.openapi.editor.Document].
         */
        internal fun normalizeSelectionLines(startLine: Int, endLine: Int, endChar: Int): Pair<Int, Int> =
            if (endChar == 0 && endLine > startLine) startLine to endLine - 1 else startLine to endLine
    }
}
