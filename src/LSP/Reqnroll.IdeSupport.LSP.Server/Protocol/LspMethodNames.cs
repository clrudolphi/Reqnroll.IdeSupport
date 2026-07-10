namespace Reqnroll.IdeSupport.LSP.Server.Protocol;

/// <summary>
/// Centralizes all LSP method names (both standard and custom Reqnroll extensions)
/// used by the language server. This prevents magic strings scattered across the codebase
/// and makes refactoring or auditing registered endpoints much easier.
/// </summary>
public static class LspMethodNames
{
    // ── Custom Reqnroll Extensions ───────────────────────────────────────────
    public const string ReqnrollProjectLoaded = "reqnroll/projectLoaded";
    public const string ReqnrollProjectUnloaded = "reqnroll/projectUnloaded";
    public const string ReqnrollProjectFiles = "reqnroll/projectFiles";
    public const string ReqnrollFindStepUsages = "reqnroll/findStepUsages";
    public const string ReqnrollGoToStepDefinitions = "reqnroll/goToStepDefinitions";
    public const string ReqnrollGoToHooks = "reqnroll/goToHooks";
    public const string ReqnrollFindUnusedStepDefinitions = "reqnroll/findUnusedStepDefinitions";
    public const string ReqnrollRenameTargets = "reqnroll/renameTargets";
    public const string ReqnrollSelectRenameTarget = "reqnroll/selectRenameTarget";
    public const string ReqnrollRefreshCodeLens = "reqnroll/refreshCodeLens";
    public const string ReqnrollSemanticTokens = "reqnroll/semanticTokens";
    public const string ReqnrollDocumentSymbolHierarchical = "reqnroll/documentSymbolHierarchical";
    public const string ReqnrollDocumentActivated = "reqnroll/documentActivated";

    // ── Standard LSP Methods ────────────────────────────────────────────────
    public const string TextDocumentSemanticTokensFull = "textDocument/semanticTokens/full";
    public const string TextDocumentSemanticTokensFullDelta = "textDocument/semanticTokens/full/delta";
    public const string TextDocumentSemanticTokensRange = "textDocument/semanticTokens/range";
    public const string TextDocumentCompletion = "textDocument/completion";
    public const string TextDocumentDefinition = "textDocument/definition";
    public const string TextDocumentReferences = "textDocument/references";
    public const string TextDocumentCodeLens = "textDocument/codeLens";
    public const string TextDocumentInlayHint = "textDocument/inlayHint";
    public const string TextDocumentFoldingRange = "textDocument/foldingRange";
    public const string TextDocumentPrepareRename = "textDocument/prepareRename";
    public const string TextDocumentRename = "textDocument/rename";
    public const string TextDocumentPublishDiagnostics = "textDocument/publishDiagnostics";
    public const string TextDocumentFormatting = "textDocument/formatting";
    public const string TextDocumentRangeFormatting = "textDocument/rangeFormatting";
    public const string TextDocumentOnTypeFormatting = "textDocument/onTypeFormatting";
    public const string TextDocumentCodeAction = "textDocument/codeAction";
    public const string TextDocumentDocumentSymbol = "textDocument/documentSymbol";
    public const string TextDocumentDidOpen = "textDocument/didOpen";
    public const string TextDocumentDidChange = "textDocument/didChange";
    public const string TextDocumentDidClose = "textDocument/didClose";

    // ── Workspace Methods ───────────────────────────────────────────────────
    public const string WorkspaceApplyEdit = "workspace/applyEdit";
    public const string WorkspaceCodeLensRefresh = "workspace/codeLens/refresh";
    public const string WorkspaceDidChangeWatchedFiles = "workspace/didChangeWatchedFiles";
    public const string WorkspaceDidChangeWorkspaceFolders = "workspace/didChangeWorkspaceFolders";
    public const string WorkspaceSemanticTokensRefresh = "workspace/semanticTokens/refresh";
    public const string WorkspaceInlayHintRefresh = "workspace/inlayHint/refresh";

    // ── Telemetry ───────────────────────────────────────────────────────────
    public const string TelemetryEvent = "telemetry/event";

    // ── Internal Pipeline Operations (not on the wire; perf-recorder labels only) ──
    public const string InternalBindingRegistryReconcile = "internal/bindingRegistryReconcile";
    public const string InternalReqnrollConfigReconcile = "internal/reqnrollConfigReconcile";
    public const string InternalFeatureRescan = "internal/featureRescan";
}
