package com.reqnroll.ide.rider.lsp.project

import com.intellij.openapi.project.Project
import com.intellij.platform.lsp.api.LspServer
import com.intellij.platform.lsp.api.LspServerManager
import com.intellij.platform.lsp.api.LspServerManagerListener
import com.intellij.platform.lsp.api.LspServerState
import com.reqnroll.ide.rider.lsp.ReqnrollLspServerSupportProvider

/**
 * Runs [action] once the Reqnroll LSP server reaches [LspServerState.Running] — immediately if
 * it's already there, otherwise deferred via [LspServerManagerListener].
 *
 * Sending any custom `reqnroll`-prefixed notification before the server completes its LSP
 * initialize/initialized handshake gets it dropped and logged as an "Unexpected notification" by
 * OmniSharp's `LspServerReceiver` — confirmed live. The server itself may not even have been
 * *started* yet: it's launched lazily from `ReqnrollLspServerSupportProvider.fileOpened`, but
 * Rider's `runnableProjectsModel.projects` reactive property (subscribed to by
 * [ReqnrollRunnableProjectsListener] and [ReqnrollProjectFilesSync]) fires on its own schedule at
 * project open, independent of any file being opened — so its very first `advise` callback almost
 * always races ahead of server startup. Route every such push through this gate rather than
 * sending directly from an `advise`/model-change callback.
 */
object ReqnrollLspServerReadiness {
    fun runWhenRunning(project: Project, action: () -> Unit) {
        val manager = LspServerManager.getInstance(project)
        val server = manager.getServersForProvider(ReqnrollLspServerSupportProvider::class.java).firstOrNull()

        if (server?.state == LspServerState.Running) {
            action()
            return
        }

        manager.addLspServerManagerListener(
            object : LspServerManagerListener {
                override fun serverStateChanged(server: LspServer) {
                    if (server.providerClass == ReqnrollLspServerSupportProvider::class.java &&
                        server.state == LspServerState.Running
                    ) {
                        action()
                    }
                }
            },
            project,
            false,
        )
    }
}
