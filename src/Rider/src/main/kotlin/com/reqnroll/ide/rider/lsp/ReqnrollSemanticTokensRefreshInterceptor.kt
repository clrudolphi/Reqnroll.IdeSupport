package com.reqnroll.ide.rider.lsp

import com.intellij.openapi.project.Project
import com.intellij.platform.lsp.api.LspServerNotificationsHandler
import com.reqnroll.ide.rider.inlayhints.ReqnrollFeatureInlayHintsController
import java.util.concurrent.CompletableFuture

/**
 * Delegates every [LspServerNotificationsHandler] callback straight through to Rider's own
 * platform-provided [handler], except [refreshSemanticTokens] — there it also refreshes this
 * project's `.feature` inlay hints ([ReqnrollFeatureInlayHintsController]) before delegating.
 *
 * The server already sends the *standard* `workspace/semanticTokens/refresh` request
 * unconditionally, for every client, the moment binding discovery changes (see
 * `SemanticTokensRefreshHandler` server-side) — reusing that existing signal avoids adding a new
 * custom `reqnroll`-prefixed protocol message that VS/VS Code clients would just ignore. Installed via
 * [ReqnrollLspServerDescriptor.createLsp4jClient].
 */
class ReqnrollSemanticTokensRefreshInterceptor(
    private val project: Project,
    private val handler: LspServerNotificationsHandler,
) : LspServerNotificationsHandler by handler {
    override fun refreshSemanticTokens(): CompletableFuture<Void> {
        ReqnrollFeatureInlayHintsController.refreshOpenFeatureEditors(project)
        return handler.refreshSemanticTokens()
    }
}
