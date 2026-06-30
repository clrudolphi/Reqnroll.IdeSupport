using Microsoft.Extensions.DependencyInjection;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Server;
using LspRange = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;
using Reqnroll.IdeSupport.LSP.Server.Features.TextSync;
using Reqnroll.IdeSupport.LSP.Server.Features.CodeActions;
using Reqnroll.IdeSupport.LSP.Server.Features.Commenting;
using Reqnroll.IdeSupport.LSP.Server.Features.DocumentOutline;
using Reqnroll.IdeSupport.LSP.Server.Features.Folding;
using Reqnroll.IdeSupport.LSP.Server.Features.Formatting;
using Reqnroll.IdeSupport.LSP.Server.Features.CodeLens;
using Reqnroll.IdeSupport.LSP.Server.Features.Completions;
using Reqnroll.IdeSupport.LSP.Server.Features.Definition;
using Reqnroll.IdeSupport.LSP.Server.Features.FindUnusedStepDefs;
using Reqnroll.IdeSupport.LSP.Server.Features.References;
using Reqnroll.IdeSupport.LSP.Server.Features.Rename;
using Reqnroll.IdeSupport.LSP.Server.Features.SemanticTokens;
using Reqnroll.IdeSupport.LSP.Server.Diagnostics.Performance;
using Reqnroll.IdeSupport.LSP.Server.Protocol;
using Reqnroll.IdeSupport.LSP.Server.Workspace;
using OmniSharp.Extensions.LanguageServer.Protocol;

namespace Reqnroll.IdeSupport.LSP.Server.Hosting;

/// <summary>
/// Extension methods for configuring custom protocol handlers on <see cref="LanguageServerOptions"/>.
/// </summary>
public static class LanguageServerOptionsExtensions
{
    /// <summary>
    /// Registers standard OmniSharp handlers. 
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
               .AddHandler<FeatureDocumentSymbolHandler>()
               .AddHandler<FeatureFoldingRangeHandler>();
    }

    /// <summary>
    /// Seeds workspace scopes and configures custom Reqnroll protocol routing.
    /// 
    /// Because OmniSharp's LSP parameter types (e.g., ReferenceParams, SemanticTokensParams) 
    /// do not implement MediatR's IRequest, we must use OnRequest/OnNotification delegates 
    /// for custom method routing. This method encapsulates the service resolution in a 
    /// strongly-typed resolver initialized during OnStarted, eliminating nullable IServiceProvider 
    /// captures and null-forgiving operators.
    /// </summary>
    public static void InitializeCustomProtocolRouting(this LanguageServerOptions options)
    {
        // Encapsulate service resolution to avoid nullable IServiceProvider? anti-pattern
        LspHandlerResolver? resolver = null;

        options.OnStarted((languageServer, _) =>
        {
            resolver = new LspHandlerResolver(languageServer.Services);
            
            var scopeManager = resolver.Get<ILspWorkspaceScopeManager>();
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

        // ── Custom client-to-server notifications (now using MediatR INotification) ─
        options.OnNotification<ReqnrollProjectLoadedParams>(
            LspMethodNames.ReqnrollProjectLoaded,
            (p, ct) => resolver!.Get<ILspWorkspaceScopeManager>().HandleProjectLoadedAsync(p, ct));

        options.OnNotification<ReqnrollProjectUnloadedParams>(
            LspMethodNames.ReqnrollProjectUnloaded,
            (p, ct) => resolver!.Get<ILspWorkspaceScopeManager>().HandleProjectUnloadedAsync(p, ct));

        options.OnNotification<ReqnrollProjectFilesParams>(
            LspMethodNames.ReqnrollProjectFiles,
            (p, ct) => resolver!.Get<ILspWorkspaceScopeManager>().HandleProjectFilesAsync(p, ct));

        // ── Manual request routing to bypass dynamic registration limitations ─────────
        // semanticTokens/full is an interactive performance target; wrap it (and its delta sibling)
        // through MeasuredAsync(...) so Layer 4 field instrumentation times the manual-route handler
        // at one site. The same helper can wrap the other manual routes below as needed.
        options.OnRequest<SemanticTokensParams, SemanticTokens>(
            LspMethodNames.TextDocumentSemanticTokensFull,
            (request, ct) => MeasuredAsync(resolver!, LspMethodNames.TextDocumentSemanticTokensFull,
                request.TextDocument.Uri, () => resolver!.Get<SemanticTokensHandler>().HandleAsync(request, ct)));

        options.OnRequest<SemanticTokensDeltaParams, SemanticTokensFullOrDelta>(
            LspMethodNames.TextDocumentSemanticTokensFullDelta,
            (request, ct) => MeasuredAsync(resolver!, LspMethodNames.TextDocumentSemanticTokensFullDelta,
                request.TextDocument.Uri, () => resolver!.Get<SemanticTokensHandler>().HandleAsync(request, ct)));

        options.OnRequest<ReferenceParams, LocationOrLocationLinks>(
            LspMethodNames.TextDocumentReferences,
            (request, ct) => resolver!.Get<StepReferencesHandler>().HandleAsync(request, ct));

        options.OnRequest<ReferenceParams, FindStepUsagesResponse>(
            LspMethodNames.ReqnrollFindStepUsages,
            (request, ct) => resolver!.Get<FindStepUsagesHandler>().HandleAsync(request, ct));

        options.OnRequest<TextDocumentPositionParams, GoToStepDefinitionsResponse>(
            LspMethodNames.ReqnrollGoToStepDefinitions,
            (request, ct) => resolver!.Get<GoToStepDefinitionsHandler>().HandleAsync(request, ct));

        options.OnRequest<TextDocumentPositionParams, GoToHooksResponse>(
            LspMethodNames.ReqnrollGoToHooks,
            (request, ct) => resolver!.Get<GoToHooksHandler>().HandleAsync(request, ct));

        options.OnRequest<CodeLensParams, CodeLens[]>(
            LspMethodNames.TextDocumentCodeLens,
            (request, ct) => resolver!.Get<StepCodeLensHandler>().HandleAsync(request, ct));

        options.OnRequest<FindUnusedStepDefinitionsParams, FindUnusedStepDefinitionsResponse>(
            LspMethodNames.ReqnrollFindUnusedStepDefinitions,
            (_, ct) => resolver!.Get<FindUnusedStepDefinitionsHandler>().HandleAsync(ct));

        // ── F16 Step Rename ────────────────────────────────────────────────────
        // prepareRename/rename handlers return null to signal "not applicable at this position".
        // OmniSharp's DelegatingRequestHandler serialises via JToken.FromObject() which throws
        // ArgumentNullException on null.  Null → throw so OmniSharp sends a JSON-RPC error
        // (clients treat an error from prepareRename/rename as "not available" and suppress the UI).
        // renameTargets null means "no targets found" → return an empty response instead.
        options.OnRequest<PrepareRenameParams, LspRange>(
            LspMethodNames.TextDocumentPrepareRename,
            async (request, ct) =>
                await resolver!.Get<StepRenameHandler>().HandlePrepareRenameAsync(request, ct)
                ?? throw new InvalidOperationException("Rename is not available at this position."));

        options.OnRequest<RenameParams, WorkspaceEdit>(
            LspMethodNames.TextDocumentRename,
            async (request, ct) =>
                await resolver!.Get<StepRenameHandler>().HandleRenameAsync(request, ct)
                ?? throw new InvalidOperationException("Rename failed — no applicable binding found."));

        options.OnRequest<TextDocumentPositionParams, RenameTargetsResponse>(
            LspMethodNames.ReqnrollRenameTargets,
            async (request, ct) =>
                await resolver!.Get<StepRenameHandler>().HandleRenameTargetsAsync(request, ct)
                ?? new RenameTargetsResponse());

        options.OnNotification<SelectRenameTargetParams>(
            LspMethodNames.ReqnrollSelectRenameTarget,
            (request, ct) => resolver!.Get<StepRenameHandler>().HandleSelectRenameTargetAsync(request, ct));
    }

    /// <summary>
    /// Times a manual-route handler invocation through the Layer 4
    /// <see cref="IOperationDurationRecorder"/>, resolved from the running server's services.
    /// </summary>
    private static async Task<T> MeasuredAsync<T>(
        LspHandlerResolver resolver, string operation, DocumentUri? uri, Func<Task<T>> body)
    {
        using var _ = resolver.Get<IOperationDurationRecorder>().Measure(operation, uri);
        return await body().ConfigureAwait(false);
    }

    /// <summary>
    /// Strongly-typed resolver for LSP handlers, eliminating nullable IServiceProvider captures.
    /// </summary>
    private sealed class LspHandlerResolver
    {
        private readonly IServiceProvider _serviceProvider;

        public LspHandlerResolver(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public T Get<T>() where T : notnull => _serviceProvider.GetRequiredService<T>();
    }
}
