package com.reqnroll.ide.rider.lsp

import com.intellij.execution.configurations.GeneralCommandLine
import com.intellij.openapi.project.Project
import com.intellij.openapi.vfs.VirtualFile
import com.intellij.platform.lsp.api.ProjectWideLspServerDescriptor
import com.reqnroll.ide.rider.logging.ReqnrollDebugLogger

class ReqnrollLspServerDescriptor(project: Project) :
    ProjectWideLspServerDescriptor(project, "Reqnroll") {

    // Unlike semantic tokens/completion/diagnostics (which default to auto-enable based
    // on server capability), the platform defaults Go To Definition support to off —
    // must opt in explicitly. FeatureDefinitionHandler/GoToStepDefinitionsHandler
    // implement it server-side. Hover isn't implemented server-side, so left disabled.
    override val lspGoToDefinitionSupport: Boolean = true

    override fun isSupportedFile(file: VirtualFile): Boolean =
        file.extension == "feature" || file.extension == "cs"

    override fun createCommandLine(): GeneralCommandLine {
        val serverPath = try {
            ReqnrollServerPathResolver.resolve()
        } catch (ex: Exception) {
            ReqnrollDebugLogger.error("createCommandLine: failed to resolve LSP server path", ex)
            throw ex
        }

        ReqnrollDebugLogger.info("createCommandLine: launching $serverPath --ide rider --log-level Warning")
        return GeneralCommandLine(serverPath.toString())
            .withParameters("--ide", "rider", "--log-level", "Warning")
    }
}
