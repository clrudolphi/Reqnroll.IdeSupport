using Microsoft.Extensions.DependencyInjection;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Server;
using Reqnroll.IdeSupport.LSP.Server.Handlers.InternalHandlers;
using Reqnroll.IdeSupport.LSP.Server.Handlers.ProtocolHandlers;
using Reqnroll.IdeSupport.LSP.Server.Protocol;
using Reqnroll.IdeSupport.LSP.Server.Workspace;

namespace Reqnroll.IdeSupport.LSP.Server;

/// <summary>
/// Extension methods for configuring custom protocol handlers and workarounds on <see cref="LanguageServerOptions"/>.
/// </summary>
public static class LanguageServerOptionsExtensions
{
    /// <summary>
    /// Registers standard OmniSharp handlers using the fluent API.
    /// </summary>
    public static void AddStandardHandlers(this LanguageServerOptions options)
    {
        options.AddHandler<TextDocumentSyncHandler>()
               .AddHandler<WorkspaceFoldersHandler>()
               .AddHandler<WatchedFilesHandler>()
               .AddHandler<FeatureDefinitionHandler>()
               .AddHandler<GherkinCompletionHandler>()
               .AddHandler<FeatureCodeActionHandler>()
               .AddHandler<GherkinFormattingHandler>()
               .AddHandler<FeatureDocumentSymbolHandler>();
    }

    /// <summary>
    /// Registers custom Reqnroll protocol handlers and manual routing workarounds.
    /// 
    /// Certain handlers (e.g., SemanticTokens, CodeLens) are registered manually via OnRequest 
    /// rather than AddHandler to bypass dynamic registration, which Visual Studio does not support. 
    /// See design doc Q13 for details.
    /// </summary>
    public static void AddReqnrollCustomProtocolHandlers(this LanguageServerOptions options)
    {
        // Capture the service provider once the container is built and the server has started.
        // This avoids null-forgiving operators on mutable fields in the outer scope.
        IServiceProvider? serviceProvider = null;

        options.OnStarted((languageServer, _) =>
        {
            serviceProvider = languageServer.Services;
            
            // Seed workspace scopes from the folders sent during the initialize handshake.
            var scopeManager = languageServer.Services.GetRequiredService<ILspWorkspaceScopeManager>();
            if (languageServer.ClientSettings.WorkspaceFolders is not null)
            {
                foreach (var folder in languageServer.ClientSettings.WorkspaceFolders)
                {
                    var path = folder.Uri.GetFileSystemPath();
                    if (!string.IsNullOrEmpty(path))
                    {
                        scopeManager.OpenWorkspace(path);
                    }
                }
            }

            return Task.CompletedTask;
        });

        // ── Custom client-to-server notifications ─────────────────────────────
        // reqnroll/projectLoaded — IDE glue sends project metadata when a project opens or rebuilds.
        options.OnNotification<ReqnrollProjectLoadedParams>(
            LspMethodNames.ReqnrollProjectLoaded,
            (p, ct) => serviceProvider!.GetRequiredService<ILspWorkspaceScopeManager>().HandleProjectLoadedAsync(p, ct));

        // reqnroll/projectUnloaded — IDE glue sends this when a project is removed.
        options.OnNotification<ReqnrollProjectUnloadedParams>(
            LspMethodNames.ReqnrollProjectUnloaded,
            (p, ct) => serviceProvider!.GetRequiredService<ILspWorkspaceScopeManager>().HandleProjectUnloadedAsync(p, ct));

        // reqnroll/projectFiles — IDE glue sends the authoritative file-membership index
        // (baseline on load/rebuild, delta on item add/remove). Drives I1/I2 invariants.
        options.OnNotification<ReqnrollProjectFilesParams>(
            LspMethodNames.ReqnrollProjectFiles,
            (p, ct) => serviceProvider!.GetRequiredService<ILspWorkspaceScopeManager>().HandleProjectFilesAsync(p, ct));

        // ── Manual routing to bypass dynamic registration limitations ─────────
        // Bypassing registering the SemanticTokensHandler as a regular handler.
        // Otherwise, it will register its capabilities dynamically, which VisualStudio doesn't support.
        // Directly wiring the handler to respond to specific request messages.
        options.OnRequest<SemanticTokensParams, SemanticTokens?>(
            LspMethodNames.TextDocumentSemanticTokensFull,
            (request, ct) => serviceProvider!.GetRequiredService<SemanticTokensHandler>().HandleAsync(request, ct));

        options.OnRequest<SemanticTokensDeltaParams, SemanticTokensFullOrDelta?>(
            LspMethodNames.TextDocumentSemanticTokensFullDelta,
            (request, ct) => serviceProvider!.GetRequiredService<SemanticTokensHandler>().HandleAsync(request, ct));

        // F14 — Find Step Definition Usages.
        // Registered manually to avoid dynamic registration ambiguity with the C# language server on .cs files.
        options.OnRequest<ReferenceParams, LocationOrLocationLinks?>(
            LspMethodNames.TextDocumentReferences,
            (request, ct) => serviceProvider!.GetRequiredService<StepReferencesHandler>().HandleAsync(request, ct));

        // F14 P2b — Custom reqnroll/findStepUsages request.
        // Delivers the full three-state contract (null / empty / locations) and per-location stepText
        // that textDocument/references cannot carry (OmniSharp LocationOrLocationLinks cannot serialize null).
        // The VS client uses this request exclusively; textDocument/references is retained for
        // spec-test compatibility and any future non-VS clients.
        options.OnRequest<ReferenceParams, FindStepUsagesResponse?>(
            LspMethodNames.ReqnrollFindStepUsages,
            (request, ct) => serviceProvider!.GetRequiredService<FindStepUsagesHandler>().HandleAsync(request, ct));

        // F5 — Go to Step Definitions (rich).
        // Custom message that returns step-type and method-name metadata alongside each location so the
        // VS extension's picker can show labelled items. The standard textDocument/definition handler
        // (FeatureDefinitionHandler) is retained for generic LSP clients.
        options.OnRequest<TextDocumentPositionParams, GoToStepDefinitionsResponse>(
            LspMethodNames.ReqnrollGoToStepDefinitions,
            (request, ct) => serviceProvider!.GetRequiredService<GoToStepDefinitionsHandler>().HandleAsync(request, ct));

        // F17 — Go to Hooks.
        // Custom message so the server can distinguish "find hooks" from F5 "find step definition"
        // on step lines, where both would fire at the same position if textDocument/definition were reused.
        options.OnRequest<TextDocumentPositionParams, GoToHooksResponse>(
            LspMethodNames.ReqnrollGoToHooks,
            (request, ct) => serviceProvider!.GetRequiredService<GoToHooksHandler>().HandleAsync(request, ct));

        // F18 — Step Code Lens.
        // Registered manually to avoid dynamic registration ambiguity with the C# language server on .cs files.
        options.OnRequest<CodeLensParams, CodeLens[]?>(
            LspMethodNames.TextDocumentCodeLens,
            (request, ct) => serviceProvider!.GetRequiredService<StepCodeLensHandler>().HandleAsync(request, ct));

        // F15 — Find Unused Step Definitions.
        // Workspace-wide command; no text-document context required.
        options.OnRequest<FindUnusedStepDefinitionsParams, FindUnusedStepDefinitionsResponse>(
            LspMethodNames.ReqnrollFindUnusedStepDefinitions,
            (_, ct) => serviceProvider!.GetRequiredService<FindUnusedStepDefinitionsHandler>().HandleAsync(ct));
    }
}
