package com.reqnroll.ide.rider.lsp

import com.intellij.openapi.project.Project
import com.intellij.platform.lsp.api.LspServerManager
import com.reqnroll.ide.rider.logging.ReqnrollDebugLogger
import com.reqnroll.ide.rider.lsp.protocol.FindStepUsagesResponse
import com.reqnroll.ide.rider.lsp.protocol.FindUnusedStepDefinitionsResponse
import com.reqnroll.ide.rider.lsp.protocol.ReqnrollEmptyParams
import com.reqnroll.ide.rider.lsp.protocol.ReqnrollLanguageServer
import org.eclipse.lsp4j.CodeLens
import org.eclipse.lsp4j.CodeLensParams
import org.eclipse.lsp4j.DocumentOnTypeFormattingParams
import org.eclipse.lsp4j.FoldingRange
import org.eclipse.lsp4j.FoldingRangeRequestParams
import org.eclipse.lsp4j.FormattingOptions
import org.eclipse.lsp4j.InlayHint
import org.eclipse.lsp4j.InlayHintParams
import org.eclipse.lsp4j.ReferenceContext
import org.eclipse.lsp4j.ReferenceParams
import org.eclipse.lsp4j.TextDocumentIdentifier
import org.eclipse.lsp4j.TextEdit
import org.eclipse.lsp4j.Position as Lsp4jPosition
import org.eclipse.lsp4j.Range as Lsp4jRange

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
    private const val CODE_LENS_TIMEOUT_MS = 10_000
    private const val INLAY_HINT_TIMEOUT_MS = 10_000
    private const val ON_TYPE_FORMATTING_TIMEOUT_MS = 10_000
    private const val FOLDING_RANGE_TIMEOUT_MS = 10_000

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

    /**
     * Runs the *standard* `textDocument/codeLens` request (step-usage-count lenses for `.cs`
     * files — see StepCodeLensHandler.cs). Standard LSP methods are already declared on LSP4J's
     * base `LanguageServer`/`TextDocumentService` interfaces, so no custom `@JsonRequest` method
     * or cast to `ReqnrollLanguageServer` is needed, unlike the custom reqnroll-prefixed methods above.
     */
    fun codeLens(project: Project, uri: String): List<CodeLens>? {
        val server = firstRunningServer(project) ?: return null
        val params = CodeLensParams(TextDocumentIdentifier(uri))
        return try {
            server.sendRequestSync(CODE_LENS_TIMEOUT_MS) { languageServer ->
                languageServer.textDocumentService.codeLens(params)
            }?.filterNotNull()
        } catch (ex: Exception) {
            ReqnrollDebugLogger.warn("codeLens: request failed", ex)
            null
        }
    }

    /** Runs the *standard* `textDocument/inlayHint` request (binding-info hints for `.feature` files — see FeatureInlayHintHandler.cs) covering the given line range. */
    fun inlayHint(project: Project, uri: String, startLine: Int, endLine: Int): List<InlayHint>? {
        val server = firstRunningServer(project) ?: return null
        val range = Lsp4jRange(Lsp4jPosition(startLine, 0), Lsp4jPosition(endLine, Int.MAX_VALUE))
        val params = InlayHintParams(TextDocumentIdentifier(uri), range)
        return try {
            server.sendRequestSync(INLAY_HINT_TIMEOUT_MS) { languageServer ->
                languageServer.textDocumentService.inlayHint(params)
            }
        } catch (ex: Exception) {
            ReqnrollDebugLogger.warn("inlayHint: request failed", ex)
            null
        }
    }

    /** Runs the *standard* `textDocument/onTypeFormatting` request (table-column realignment for `.feature` files — see GherkinFormattingHandler.cs) at (line, character), triggered by typing [triggerChar]. */
    fun onTypeFormatting(
        project: Project,
        uri: String,
        line: Int,
        character: Int,
        triggerChar: String,
        tabSize: Int,
        insertSpaces: Boolean,
    ): List<TextEdit>? {
        val server = firstRunningServer(project) ?: return null
        val params = DocumentOnTypeFormattingParams(
            TextDocumentIdentifier(uri),
            FormattingOptions(tabSize, insertSpaces),
            Lsp4jPosition(line, character),
            triggerChar,
        )
        return try {
            server.sendRequestSync(ON_TYPE_FORMATTING_TIMEOUT_MS) { languageServer ->
                languageServer.textDocumentService.onTypeFormatting(params)
            }?.filterNotNull()
        } catch (ex: Exception) {
            ReqnrollDebugLogger.warn("onTypeFormatting: request failed", ex)
            null
        }
    }

    /**
     * Runs the *standard* `textDocument/foldingRange` request (Code Folding for `.feature`
     * files — see FeatureFoldingRangeHandler.cs). Standard LSP method, so — like [codeLens] and
     * [inlayHint] — no custom `@JsonRequest` method or cast to `ReqnrollLanguageServer` is needed.
     */
    fun foldingRange(project: Project, uri: String): List<FoldingRange>? {
        val server = firstRunningServer(project) ?: return null
        val params = FoldingRangeRequestParams(TextDocumentIdentifier(uri))
        return try {
            server.sendRequestSync(FOLDING_RANGE_TIMEOUT_MS) { languageServer ->
                languageServer.textDocumentService.foldingRange(params)
            }?.filterNotNull()
        } catch (ex: Exception) {
            ReqnrollDebugLogger.warn("foldingRange: request failed", ex)
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
