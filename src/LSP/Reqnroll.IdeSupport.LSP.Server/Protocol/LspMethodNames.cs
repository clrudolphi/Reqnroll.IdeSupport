namespace Reqnroll.IdeSupport.LSP.Server.Protocol;

/// <summary>
/// Centralizes all LSP method names (both standard and custom Reqnroll extensions)
/// used by the language server. This prevents magic strings scattered across the codebase
/// and makes refactoring or auditing registered endpoints much easier.
/// </summary>
public static class LspMethodNames
{
    // ── Custom Reqnroll Extensions ───────────────────────────────────────────
    /// <summary>Gets or sets the reqnroll project loaded.</summary>
    public const string ReqnrollProjectLoaded = "reqnroll/projectLoaded";
    /// <summary>Gets or sets the reqnroll project unloaded.</summary>
    public const string ReqnrollProjectUnloaded = "reqnroll/projectUnloaded";
    /// <summary>Gets or sets the reqnroll project files.</summary>
    public const string ReqnrollProjectFiles = "reqnroll/projectFiles";
    /// <summary>Gets or sets the reqnroll find step usages.</summary>
    public const string ReqnrollFindStepUsages = "reqnroll/findStepUsages";
    /// <summary>Gets or sets the reqnroll go to step definitions.</summary>
    public const string ReqnrollGoToStepDefinitions = "reqnroll/goToStepDefinitions";
    /// <summary>Gets or sets the reqnroll go to hooks.</summary>
    public const string ReqnrollGoToHooks = "reqnroll/goToHooks";
    /// <summary>Gets or sets the reqnroll find unused step definitions.</summary>
    public const string ReqnrollFindUnusedStepDefinitions = "reqnroll/findUnusedStepDefinitions";
    /// <summary>Gets or sets the reqnroll rename targets.</summary>
    public const string ReqnrollRenameTargets = "reqnroll/renameTargets";
    /// <summary>Gets or sets the reqnroll select rename target.</summary>
    public const string ReqnrollSelectRenameTarget = "reqnroll/selectRenameTarget";
    /// <summary>Gets or sets the reqnroll refresh code lens.</summary>
    public const string ReqnrollRefreshCodeLens = "reqnroll/refreshCodeLens";
    /// <summary>Gets or sets the reqnroll semantic tokens.</summary>
    public const string ReqnrollSemanticTokens = "reqnroll/semanticTokens";
    /// <summary>Gets or sets the reqnroll document symbol hierarchical.</summary>
    public const string ReqnrollDocumentSymbolHierarchical = "reqnroll/documentSymbolHierarchical";
    /// <summary>Gets or sets the reqnroll document activated.</summary>
    public const string ReqnrollDocumentActivated = "reqnroll/documentActivated";

    // ── Standard LSP Methods ────────────────────────────────────────────────
    /// <summary>Gets or sets the text document semantic tokens full.</summary>
    public const string TextDocumentSemanticTokensFull = "textDocument/semanticTokens/full";
    /// <summary>Gets or sets the text document semantic tokens full delta.</summary>
    public const string TextDocumentSemanticTokensFullDelta = "textDocument/semanticTokens/full/delta";
    /// <summary>Gets or sets the text document semantic tokens range.</summary>
    public const string TextDocumentSemanticTokensRange = "textDocument/semanticTokens/range";
    /// <summary>Gets or sets the text document completion.</summary>
    public const string TextDocumentCompletion = "textDocument/completion";
    /// <summary>Gets or sets the text document definition.</summary>
    public const string TextDocumentDefinition = "textDocument/definition";
    /// <summary>Gets or sets the text document references.</summary>
    public const string TextDocumentReferences = "textDocument/references";
    /// <summary>Gets or sets the text document code lens.</summary>
    public const string TextDocumentCodeLens = "textDocument/codeLens";
    /// <summary>Gets or sets the text document inlay hint.</summary>
    public const string TextDocumentInlayHint = "textDocument/inlayHint";
    /// <summary>Gets or sets the text document folding range.</summary>
    public const string TextDocumentFoldingRange = "textDocument/foldingRange";
    /// <summary>Gets or sets the text document prepare rename.</summary>
    public const string TextDocumentPrepareRename = "textDocument/prepareRename";
    /// <summary>Gets or sets the text document rename.</summary>
    public const string TextDocumentRename = "textDocument/rename";
    /// <summary>Gets or sets the text document publish diagnostics.</summary>
    public const string TextDocumentPublishDiagnostics = "textDocument/publishDiagnostics";
    /// <summary>Gets or sets the text document formatting.</summary>
    public const string TextDocumentFormatting = "textDocument/formatting";
    /// <summary>Gets or sets the text document range formatting.</summary>
    public const string TextDocumentRangeFormatting = "textDocument/rangeFormatting";
    /// <summary>Gets or sets the text document on type formatting.</summary>
    public const string TextDocumentOnTypeFormatting = "textDocument/onTypeFormatting";
    /// <summary>Gets or sets the text document code action.</summary>
    public const string TextDocumentCodeAction = "textDocument/codeAction";
    /// <summary>Gets or sets the text document document symbol.</summary>
    public const string TextDocumentDocumentSymbol = "textDocument/documentSymbol";
    /// <summary>Gets or sets the text document did open.</summary>
    public const string TextDocumentDidOpen = "textDocument/didOpen";
    /// <summary>Gets or sets the text document did change.</summary>
    public const string TextDocumentDidChange = "textDocument/didChange";
    /// <summary>Gets or sets the text document did close.</summary>
    public const string TextDocumentDidClose = "textDocument/didClose";

    // ── Workspace Methods ───────────────────────────────────────────────────
    /// <summary>Gets or sets the workspace apply edit.</summary>
    public const string WorkspaceApplyEdit = "workspace/applyEdit";
    /// <summary>Gets or sets the workspace code lens refresh.</summary>
    public const string WorkspaceCodeLensRefresh = "workspace/codeLens/refresh";
    /// <summary>Gets or sets the workspace did change watched files.</summary>
    public const string WorkspaceDidChangeWatchedFiles = "workspace/didChangeWatchedFiles";
    /// <summary>Gets or sets the workspace did change workspace folders.</summary>
    public const string WorkspaceDidChangeWorkspaceFolders = "workspace/didChangeWorkspaceFolders";
    /// <summary>Gets or sets the workspace semantic tokens refresh.</summary>
    public const string WorkspaceSemanticTokensRefresh = "workspace/semanticTokens/refresh";
    /// <summary>Gets or sets the workspace inlay hint refresh.</summary>
    public const string WorkspaceInlayHintRefresh = "workspace/inlayHint/refresh";

    // ── Telemetry ───────────────────────────────────────────────────────────
    /// <summary>Gets or sets the telemetry event.</summary>
    public const string TelemetryEvent = "telemetry/event";

    // ── Internal Pipeline Operations (not on the wire; perf-recorder labels only) ──
    /// <summary>Gets or sets the internal binding registry reconcile.</summary>
    public const string InternalBindingRegistryReconcile = "internal/bindingRegistryReconcile";
    /// <summary>Gets or sets the internal reqnroll config reconcile.</summary>
    public const string InternalReqnrollConfigReconcile = "internal/reqnrollConfigReconcile";
    /// <summary>Gets or sets the internal feature rescan.</summary>
    public const string InternalFeatureRescan = "internal/featureRescan";
}
