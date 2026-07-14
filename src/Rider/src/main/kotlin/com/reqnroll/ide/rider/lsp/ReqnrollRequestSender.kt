package com.reqnroll.ide.rider.lsp

import com.intellij.openapi.project.Project
import com.intellij.platform.lsp.api.LspServerManager
import com.reqnroll.ide.rider.logging.ReqnrollDebugLogger
import com.reqnroll.ide.rider.lsp.protocol.FindStepUsagesResponse
import com.reqnroll.ide.rider.lsp.protocol.FindUnusedStepDefinitionsResponse
import com.reqnroll.ide.rider.lsp.protocol.ReqnrollEmptyParams
import com.reqnroll.ide.rider.lsp.protocol.ReqnrollLanguageServer
import org.eclipse.lsp4j.ReferenceContext
import org.eclipse.lsp4j.ReferenceParams
import org.eclipse.lsp4j.TextDocumentIdentifier
import org.eclipse.lsp4j.Position as Lsp4jPosition

/**
 * Sends the reqnroll-prefixed client-to-server *requests* (as opposed to
 * [ReqnrollNotificationSender]'s fire-and-forget notifications) via `LspServer.sendRequestSync`,
 * confirmed to exist on Rider 2024.3.5's actual `LspServer` interface by decompiling
 * `com.intellij.platform.lsp.api.LspServer`. `sendRequestSync` blocks the calling thread until a
 * response arrives or the timeout elapses — callers must invoke this from a background thread
 * (e.g. inside a `Task.Backgroundable`), never directly from `AnAction.actionPerformed`'s EDT
 * dispatch.
 */
object ReqnrollRequestSender {
    private const val FIND_UNUSED_TIMEOUT_MS = 30_000
    private const val FIND_USAGES_TIMEOUT_MS = 10_000

    /** Runs `reqnroll/findUnusedStepDefinitions`. Returns null if no Reqnroll LSP server is running, or on failure. */
    fun findUnusedStepDefinitions(project: Project): FindUnusedStepDefinitionsResponse? {
        val server = firstRunningServer(project) ?: return null
        return try {
            server.sendRequestSync(FIND_UNUSED_TIMEOUT_MS) { languageServer ->
                (languageServer as ReqnrollLanguageServer).findUnusedStepDefinitions(ReqnrollEmptyParams())
            }
        } catch (ex: Exception) {
            ReqnrollDebugLogger.warn("findUnusedStepDefinitions: request failed", ex)
            null
        }
    }

    /** Runs `reqnroll/findStepUsages` for the binding at (uri, line, character). Returns null if no Reqnroll LSP server is running, or on failure. */
    fun findStepUsages(project: Project, uri: String, line: Int, character: Int): FindStepUsagesResponse? {
        val server = firstRunningServer(project) ?: return null
        val params = ReferenceParams().apply {
            textDocument = TextDocumentIdentifier(uri)
            position = Lsp4jPosition(line, character)
            context = ReferenceContext(false)
        }
        return try {
            server.sendRequestSync(FIND_USAGES_TIMEOUT_MS) { languageServer ->
                (languageServer as ReqnrollLanguageServer).findStepUsages(params)
            }
        } catch (ex: Exception) {
            ReqnrollDebugLogger.warn("findStepUsages: request failed", ex)
            null
        }
    }

    private fun firstRunningServer(project: Project) =
        LspServerManager.getInstance(project)
            .getServersForProvider(ReqnrollLspServerSupportProvider::class.java)
            .firstOrNull()
            .also {
                if (it == null)
                    ReqnrollDebugLogger.warn("ReqnrollRequestSender: no Reqnroll LSP server running")
            }
}
