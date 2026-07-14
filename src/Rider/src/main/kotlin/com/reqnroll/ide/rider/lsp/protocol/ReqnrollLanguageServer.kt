package com.reqnroll.ide.rider.lsp.protocol

import org.eclipse.lsp4j.ReferenceParams
import org.eclipse.lsp4j.jsonrpc.services.JsonNotification
import org.eclipse.lsp4j.jsonrpc.services.JsonRequest
import org.eclipse.lsp4j.services.LanguageServer
import java.util.concurrent.CompletableFuture

/**
 * Custom LSP4J server interface adding the Reqnroll protocol extensions
 * (src/LSP/Reqnroll.IdeSupport.LSP.Server/Protocol/LspMethodNames.cs) that the platform's
 * generic LSP client has no built-in way to send. Wired in via
 * ReqnrollLspServerDescriptor.lsp4jServerClass; see
 * docs/Rider-Project-Document-Sync-Implementation-Plan.md §3.1 for how the resulting typed
 * proxy is obtained and called (LspServerManager + LspServer.sendNotification), confirmed
 * against Rider 2024.3.5's actual bundled classes, not just JetBrains' docs.
 *
 * `@JsonRequest` methods (as opposed to `@JsonNotification`) are the request/response
 * counterpart — called via `LspServer.sendRequest`/`sendRequestSync` instead of
 * `sendNotification`, confirmed to exist on Rider 2024.3.5's actual `LspServer` interface by
 * decompiling `com.intellij.platform.lsp.api.LspServer`.
 */
interface ReqnrollLanguageServer : LanguageServer {
    @JsonNotification("reqnroll/projectLoaded")
    fun projectLoaded(params: ReqnrollProjectLoadedParams)

    @JsonNotification("reqnroll/projectUnloaded")
    fun projectUnloaded(params: ReqnrollProjectUnloadedParams)

    @JsonNotification("reqnroll/projectFiles")
    fun projectFiles(params: ReqnrollProjectFilesParams)

    @JsonNotification("reqnroll/documentActivated")
    fun documentActivated(params: DocumentActivatedParams)

    /** Find Unused Step Definitions (F15) — scans the whole workspace, no params. */
    @JsonRequest("reqnroll/findUnusedStepDefinitions")
    fun findUnusedStepDefinitions(params: ReqnrollEmptyParams): CompletableFuture<FindUnusedStepDefinitionsResponse>

    /**
     * Find Step Definition Usages — params are the standard `textDocument/references` shape
     * (`ReferenceParams`, an existing LSP4J class) even though this is a custom method name; the
     * server registers it that way deliberately (see FindStepUsagesHandler.cs) since it needs a
     * third "not a binding" state generic `textDocument/references` can't express.
     */
    @JsonRequest("reqnroll/findStepUsages")
    fun findStepUsages(params: ReferenceParams): CompletableFuture<FindStepUsagesResponse>
}
