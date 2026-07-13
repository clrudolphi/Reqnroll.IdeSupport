package com.reqnroll.ide.rider.lsp

import com.intellij.openapi.project.Project
import com.intellij.openapi.vfs.VirtualFile
import com.intellij.platform.lsp.api.LspServerSupportProvider

class ReqnrollLspServerSupportProvider : LspServerSupportProvider {
    override fun fileOpened(
        project: Project,
        file: VirtualFile,
        serverStarter: LspServerSupportProvider.LspServerStarter,
    ) {
        if (file.extension != "feature" && file.extension != "cs") {
            return
        }

        serverStarter.ensureServerStarted(ReqnrollLspServerDescriptor(project))
    }
}
