package com.reqnroll.ide.rider.lsp

import com.intellij.openapi.project.Project
import com.intellij.openapi.vfs.VirtualFile
import com.intellij.platform.lsp.api.LspServerSupportProvider
import com.reqnroll.ide.rider.logging.ReqnrollDebugLogger

class ReqnrollLspServerSupportProvider : LspServerSupportProvider {
    override fun fileOpened(
        project: Project,
        file: VirtualFile,
        serverStarter: LspServerSupportProvider.LspServerStarter,
    ) {
        if (file.extension != "feature" && file.extension != "cs") {
            return
        }

        ReqnrollDebugLogger.info("fileOpened: starting/reusing LSP server for ${file.path}")
        serverStarter.ensureServerStarted(ReqnrollLspServerDescriptor(project))
    }
}
