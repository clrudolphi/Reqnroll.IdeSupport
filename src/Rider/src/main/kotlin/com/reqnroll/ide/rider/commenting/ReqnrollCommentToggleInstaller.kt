package com.reqnroll.ide.rider.commenting

import com.intellij.openapi.actionSystem.IdeActions
import com.intellij.openapi.editor.actionSystem.EditorActionManager
import com.intellij.openapi.project.Project
import com.intellij.openapi.startup.ProjectActivity
import java.util.concurrent.atomic.AtomicBoolean

/**
 * Installs [ReqnrollCommentToggleHandler] over IntelliJ's built-in `CommentByLineComment` action
 * handler (issue #159). `EditorActionManager` is application-global, not per-project, so this
 * only needs to run once — [installed] guards against re-wrapping on every project open (a
 * `postStartupActivity` fires once per opened project, and re-wrapping an already-wrapped handler
 * would nest a nonfunctional (but harmless) extra layer of delegation each time).
 */
class ReqnrollCommentToggleInstaller : ProjectActivity {
    override suspend fun execute(project: Project) {
        if (!installed.compareAndSet(false, true)) return

        val actionManager = EditorActionManager.getInstance()
        val original = actionManager.getActionHandler(IdeActions.ACTION_COMMENT_LINE)
        actionManager.setActionHandler(IdeActions.ACTION_COMMENT_LINE, ReqnrollCommentToggleHandler(original))
    }

    private companion object {
        val installed = AtomicBoolean(false)
    }
}
