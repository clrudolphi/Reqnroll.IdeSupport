package com.reqnroll.ide.rider.lsp.protocol

import org.eclipse.lsp4j.jsonrpc.services.JsonNotification
import org.eclipse.lsp4j.services.LanguageServer

/**
 * Custom LSP4J server interface adding the Reqnroll protocol extensions
 * (src/LSP/Reqnroll.IdeSupport.LSP.Server/Protocol/LspMethodNames.cs) that the platform's
 * generic LSP client has no built-in way to send. Wired in via
 * ReqnrollLspServerDescriptor.lsp4jServerClass; see
 * docs/Rider-Project-Document-Sync-Implementation-Plan.md §3.1 for how the resulting typed
 * proxy is obtained and called (LspServerManager + LspServer.sendNotification), confirmed
 * against Rider 2024.3.5's actual bundled classes, not just JetBrains' docs.
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
}
