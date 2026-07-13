package com.reqnroll.ide.rider.lsp

import com.intellij.execution.configurations.GeneralCommandLine
import com.intellij.openapi.project.Project
import com.intellij.openapi.vfs.VirtualFile
import com.intellij.platform.lsp.api.ProjectWideLspServerDescriptor
import com.reqnroll.ide.rider.logging.ReqnrollDebugLogger
import com.reqnroll.ide.rider.lsp.protocol.ReqnrollLanguageServer
import org.eclipse.lsp4j.services.LanguageServer

class ReqnrollLspServerDescriptor(project: Project) :
    ProjectWideLspServerDescriptor(project, "Reqnroll") {

    // Unlike semantic tokens/completion/diagnostics (which default to auto-enable based
    // on server capability), the platform defaults Go To Definition support to off —
    // must opt in explicitly. FeatureDefinitionHandler/GoToStepDefinitionsHandler
    // implement it server-side. Hover isn't implemented server-side, so left disabled.
    override val lspGoToDefinitionSupport: Boolean = true

    // Adds the reqnroll-prefixed client-to-server notifications (ReqnrollNotificationSender) on top
    // of the standard LanguageServer interface. Confirmed against Rider 2024.3.5's actual
    // bundled LspServerDescriptor.class that this property genuinely exists and is overridable
    // at this platform version — see docs/Rider-Project-Document-Sync-Implementation-Plan.md.
    // Kotlin `open val` in the superclass — must override as `val`, not `fun getXxx()`, even
    // though the compiled superclass's JVM member is a getLsp4jServerClass() method.
    override val lsp4jServerClass: Class<out LanguageServer> = ReqnrollLanguageServer::class.java

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
