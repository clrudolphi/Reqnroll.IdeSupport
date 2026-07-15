package com.reqnroll.ide.rider.lsp

import com.intellij.openapi.project.Project
import com.intellij.platform.lsp.api.LspServerNotificationsHandler
import com.reqnroll.ide.rider.codevision.StepUsagesCodeVisionProvider
import java.util.concurrent.CompletableFuture

/**
 * Delegates every [LspServerNotificationsHandler] callback straight through to Rider's own
 * platform-provided [handler], except [refreshCodeLenses] — there it also refreshes this
 * project's "N step usages" CodeVision lens ([StepUsagesCodeVisionProvider]) before delegating.
 *
 * Rider's CodeVision engine has no signal of its own for "the data behind this lens changed" —
 * unlike inlay hints/semantic tokens, which at least have *a* refresh mechanism once wired (see
 * [ReqnrollInlayHintRefreshInterceptor]), a stale-until-you-edit-the-.cs-file lens count was the
 * actual reported bug this fixes. Installed via [ReqnrollLspServerDescriptor.createLsp4jClient].
 */
class ReqnrollCodeLensRefreshInterceptor(
    private val project: Project,
    private val handler: LspServerNotificationsHandler,
) : LspServerNotificationsHandler by handler {
    override fun refreshCodeLenses(): CompletableFuture<Void> {
        StepUsagesCodeVisionProvider.refreshOpenCsEditors(project)
        return handler.refreshCodeLenses()
    }
}
