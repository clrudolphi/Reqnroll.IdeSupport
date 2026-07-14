package com.reqnroll.ide.rider.lsp

import com.intellij.execution.configurations.GeneralCommandLine
import com.intellij.openapi.project.Project
import com.intellij.openapi.vfs.VirtualFile
import com.intellij.platform.lsp.api.Lsp4jClient
import com.intellij.platform.lsp.api.LspServerNotificationsHandler
import com.intellij.platform.lsp.api.ProjectWideLspServerDescriptor
import com.intellij.platform.lsp.api.customization.LspSemanticTokensSupport
import com.reqnroll.ide.rider.logging.ReqnrollDebugLogger
import com.reqnroll.ide.rider.lsp.protocol.ReqnrollLanguageServer
import com.reqnroll.ide.rider.lsp.semantictokens.ReqnrollSemanticTokensSupport
import org.eclipse.lsp4j.services.LanguageServer

/**
 * Central configuration point for the Reqnroll LSP server as far as Rider's generic
 * `com.intellij.platform.lsp.api` client is concerned: which files it covers, how to launch it,
 * and which optional client-side customizations (go to definition, semantic token coloring, the
 * custom reqnroll-prefixed protocol extensions) to enable. One instance is created per project by
 * [ReqnrollLspServerSupportProvider].
 */
class ReqnrollLspServerDescriptor(project: Project) :
    ProjectWideLspServerDescriptor(project, "Reqnroll") {

    // Unlike semantic tokens/completion/diagnostics (which default to auto-enable based
    // on server capability), the platform defaults Go To Definition support to off —
    // must opt in explicitly. FeatureDefinitionHandler/GoToStepDefinitionsHandler
    // implement it server-side. Hover isn't implemented server-side, so left disabled.
    override val lspGoToDefinitionSupport: Boolean = true

    // Without this, Rider's platform default only colors the LSP *standard* token type
    // vocabulary (confirmed by decompiling LspSemanticTokensSupport.getTextAttributesKey — a
    // fixed switch over ~23 hardcoded names) and silently ignores our custom `reqnroll.*`
    // names — same class of problem VS's built-in colorizer has. See
    // com/reqnroll/ide/rider/lsp/semantictokens/ReqnrollSemanticTokensSupport.kt.
    override val lspSemanticTokensSupport: LspSemanticTokensSupport = ReqnrollSemanticTokensSupport()

    // Adds the reqnroll-prefixed client-to-server notifications (ReqnrollNotificationSender) on top
    // of the standard LanguageServer interface. Confirmed against Rider 2024.3.5's actual
    // bundled LspServerDescriptor.class that this property genuinely exists and is overridable
    // at this platform version — see docs/Rider-Project-Document-Sync-Implementation-Plan.md.
    // Kotlin `open val` in the superclass — must override as `val`, not `fun getXxx()`, even
    // though the compiled superclass's JVM member is a getLsp4jServerClass() method.
    override val lsp4jServerClass: Class<out LanguageServer> = ReqnrollLanguageServer::class.java

    /** This server covers `.feature` files (Gherkin) and `.cs` files (step-definition bindings). */
    override fun isSupportedFile(file: VirtualFile): Boolean =
        file.extension == "feature" || file.extension == "cs"

    /** Wraps the platform's own handler so a `workspace/semanticTokens/refresh` also refreshes `.feature` inlay hints — see [ReqnrollSemanticTokensRefreshInterceptor]. */
    override fun createLsp4jClient(serverNotificationsHandler: LspServerNotificationsHandler): Lsp4jClient =
        Lsp4jClient(ReqnrollSemanticTokensRefreshInterceptor(project, serverNotificationsHandler))

    /** Resolves the bundled server executable and builds the launch command line, with the log level chosen per [resolveLogLevel]. */
    override fun createCommandLine(): GeneralCommandLine {
        val serverPath = try {
            ReqnrollServerPathResolver.resolve()
        } catch (ex: Exception) {
            ReqnrollDebugLogger.error("createCommandLine: failed to resolve LSP server path", ex)
            throw ex
        }

        // "reqnroll.devSandbox" is set by build.gradle.kts only on the JVM that `runIde` launches
        // (a local dev sandbox) — never present in a real installed plugin — so local dev gets
        // full diagnostic logging without needing a manual override every time.
        val logLevel = resolveLogLevel(System.getProperty("reqnroll.devSandbox") == "true")

        ReqnrollDebugLogger.info("createCommandLine: launching $serverPath --ide rider --log-level $logLevel")
        return GeneralCommandLine(serverPath.toString())
            .withParameters("--ide", "rider", "--log-level", logLevel)
    }

    companion object {
        /** Pure/parameterized so it's testable without mutating the real System property — see ReqnrollServerPathResolver's identical rationale. */
        internal fun resolveLogLevel(isDevSandbox: Boolean): String =
            if (isDevSandbox) "Verbose" else "Warning"
    }
}
