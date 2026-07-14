package com.reqnroll.ide.rider.lsp

import com.intellij.openapi.project.Project
import com.intellij.openapi.vfs.VirtualFile
import com.intellij.platform.lsp.api.LspServerSupportProvider
import com.reqnroll.ide.rider.logging.ReqnrollDebugLogger
import com.reqnroll.ide.rider.lsp.project.ReqnrollProjectBaseline

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

        // Closes a race the project/document-sync ProjectActivity listeners can't close on their
        // own: they run at project open (independent of any file being opened) and their initial
        // advise() fire almost always finds no server running yet, since server startup itself is
        // gated on this very fileOpened callback — that first push is silently dropped, and unlike
        // VS (which has its own preload side channel for the same problem), there's no guarantee
        // anything re-triggers those listeners' advise callback again in the same session. Re-push
        // the current snapshot now that the server is guaranteed to exist; safe/idempotent even if
        // the listeners' own push already succeeded (the server treats a repeat baseline for an
        // already-loaded project as an update, not a duplicate).
        ReqnrollProjectBaseline.pushForAllRunnableProjects(project)
    }
}
