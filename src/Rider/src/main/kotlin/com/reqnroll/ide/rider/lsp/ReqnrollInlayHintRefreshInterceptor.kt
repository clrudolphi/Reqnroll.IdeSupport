package com.reqnroll.ide.rider.lsp

import com.intellij.openapi.project.Project
import com.intellij.platform.lsp.api.LspServerNotificationsHandler
import com.reqnroll.ide.rider.breadcrumbs.ReqnrollFeatureBreadcrumbsCollector
import com.reqnroll.ide.rider.folding.ReqnrollFeatureFoldingController
import com.reqnroll.ide.rider.inlayhints.ReqnrollFeatureInlayHintsController
import com.reqnroll.ide.rider.structureview.ReqnrollStructurePanel
import java.util.concurrent.CompletableFuture

/**
 * Delegates every [LspServerNotificationsHandler] callback straight through to Rider's own
 * platform-provided [handler], except [refreshInlayHints] — there it also refreshes this
 * project's `.feature` inlay hints ([ReqnrollFeatureInlayHintsController]), folding ranges
 * ([ReqnrollFeatureFoldingController]), breadcrumbs ([ReqnrollFeatureBreadcrumbsCollector]), and
 * Structure View tool window ([ReqnrollStructurePanel]) before delegating.
 *
 * The server already sends the *standard* `workspace/inlayHint/refresh` request — purpose-built
 * for exactly this — debounced, whenever binding discovery changes and the client advertised
 * `workspace.inlayHint.refreshSupport` (see `InlayHintRefreshHandler` server-side). No new
 * `reqnroll`-prefixed protocol message needed. Installed via
 * [ReqnrollLspServerDescriptor.createLsp4jClient].
 *
 * Folding, breadcrumbs, and Structure View have no `workspace/foldingRange/refresh` or equivalent
 * `documentSymbol` refresh request in the LSP spec, so all three piggyback on this same signal
 * rather than getting their own interceptor — see [ReqnrollFeatureFoldingController]'s,
 * [ReqnrollFeatureBreadcrumbsCollector]'s, and [ReqnrollStructurePanel]'s class docs for why each
 * needs one at all.
 */
class ReqnrollInlayHintRefreshInterceptor(
    private val project: Project,
    private val handler: LspServerNotificationsHandler,
) : LspServerNotificationsHandler by handler {
    override fun refreshInlayHints(): CompletableFuture<Void> {
        ReqnrollFeatureInlayHintsController.refreshOpenFeatureEditors(project)
        ReqnrollFeatureFoldingController.refreshOpenFeatureEditors(project)
        ReqnrollFeatureBreadcrumbsCollector.refreshOpenFeatureEditors(project)
        ReqnrollStructurePanel.refreshActivePanel(project)
        return handler.refreshInlayHints()
    }
}
