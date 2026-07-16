package com.reqnroll.ide.rider.lsp

import com.intellij.openapi.project.Project
import com.intellij.platform.lsp.api.LspServerNotificationsHandler
import com.reqnroll.ide.rider.folding.ReqnrollFeatureFoldingController
import com.reqnroll.ide.rider.inlayhints.ReqnrollFeatureInlayHintsController
import java.util.concurrent.CompletableFuture

/**
 * Delegates every [LspServerNotificationsHandler] callback straight through to Rider's own
 * platform-provided [handler], except [refreshInlayHints] — there it also refreshes this
 * project's `.feature` inlay hints ([ReqnrollFeatureInlayHintsController]) and folding ranges
 * ([ReqnrollFeatureFoldingController]) before delegating.
 *
 * The server already sends the *standard* `workspace/inlayHint/refresh` request — purpose-built
 * for exactly this — debounced, whenever binding discovery changes and the client advertised
 * `workspace.inlayHint.refreshSupport` (see `InlayHintRefreshHandler` server-side). No new
 * `reqnroll`-prefixed protocol message needed. Installed via
 * [ReqnrollLspServerDescriptor.createLsp4jClient].
 *
 * Folding has no `workspace/foldingRange/refresh` request in the LSP spec, so it piggybacks on
 * this same signal rather than getting its own interceptor — see
 * [ReqnrollFeatureFoldingController]'s class doc for why it needs one at all.
 */
class ReqnrollInlayHintRefreshInterceptor(
    private val project: Project,
    private val handler: LspServerNotificationsHandler,
) : LspServerNotificationsHandler by handler {
    override fun refreshInlayHints(): CompletableFuture<Void> {
        ReqnrollFeatureInlayHintsController.refreshOpenFeatureEditors(project)
        ReqnrollFeatureFoldingController.refreshOpenFeatureEditors(project)
        return handler.refreshInlayHints()
    }
}
