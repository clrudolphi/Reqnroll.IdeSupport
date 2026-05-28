using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Server;
using Reqnroll.IdeSupport.Common;
using Reqnroll.IdeSupport.Common.Diagnostics;
using Reqnroll.IdeSupport.Common.ProjectSystem.Configuration;
using Reqnroll.IdeSupport.LSP.Core.Discovery;
using Reqnroll.IdeSupport.LSP.Core.Editor.Services.Parsing.GherkinDocuments;
using Reqnroll.IdeSupport.LSP.Server.Diagnostics;
using Reqnroll.IdeSupport.LSP.Server.Discovery;
using Reqnroll.IdeSupport.LSP.Server.Handlers.InternalHandlers;
using Reqnroll.IdeSupport.LSP.Server.Handlers.ProtocolHandlers;
using Reqnroll.IdeSupport.LSP.Server.Notifications;
using Reqnroll.IdeSupport.LSP.Server.Services;
using Reqnroll.IdeSupport.LSP.Server.Workspace;
using System.Diagnostics;
using System.Reactive;

namespace Reqnroll.IdeSupport.LSP.Server;

public class Program
{
    public static async Task Main(string[] args)
    {
        var server = await LanguageServer.From(ConfigureServer).ConfigureAwait(false);
        await server.WaitForExit.ConfigureAwait(false);
    }

    private static void ConfigureServer(LanguageServerOptions options)
    {
        IServiceProvider? serverServices = null;

        options.WithInput(Console.OpenStandardInput())
               .WithOutput(Console.OpenStandardOutput());

        options.ConfigureLogging(logging =>
        {
            logging.SetMinimumLevel(LogLevel.Trace);
            logging.AddLanguageProtocolLogging();
        });

        options.WithServerInfo(new ServerInfo
        {
            Name = "Reqnroll Language Server",
            Version = "0.1.0"
        });

        options.Services
               .AddMediatR(typeof(Program))
               .AddSingleton<IDeveroomLogger, LspDeveroomLogger>()
               .AddSingleton<IIdeScope, LspIdeScope>()
               .AddSingleton<IMonitoringService>(sp => NullMonitoringService.Instance)
               .AddSingleton<IDeveroomConfigurationProvider, ProjectSystemDeveroomConfigurationProvider>()
               .AddSingleton<ILspWorkspaceScopeManager, LspWorkspaceScopeManager>()
               .AddSingleton<IBindingRegistryProvider, NullBindingRegistryProvider>()
               .AddSingleton<IDeveroomTagParser, DeveroomTagParser>()
               .AddSingleton<IDocumentBufferService, DocumentBufferService>()
               .AddSingleton<IGherkinDocumentTaggerService, GherkinDocumentTaggerService>()
               .AddSingleton<ISemanticTokenService, SemanticTokenService>()
               // SemanticTokensRefreshHandler is registered both as itself (singleton)
               // and as the MediatR INotificationHandler so the same instance handles all notifications.
               .AddSingleton<SemanticTokensRefreshHandler>()
               .AddSingleton<INotificationHandler<GherkinDocumentParsedNotification>>(
                   sp => sp.GetRequiredService<SemanticTokensRefreshHandler>())
               // ReqnrollConfigChangedHandler is registered both as itself and as the MediatR handler.
               .AddSingleton<ReqnrollConfigChangedHandler>()
               .AddSingleton<INotificationHandler<ReqnrollConfigChangedNotification>>(
                   sp => sp.GetRequiredService<ReqnrollConfigChangedHandler>())
               // Handlers must be pre-registered as singletons so DryIoc can resolve
               // them without an open scope (TrackingDisposableTransients rule).
               .AddSingleton<TextDocumentSyncHandler>()
               .AddSingleton<WorkspaceFoldersHandler>()
               .AddSingleton<WatchedFilesHandler>()
               .AddSingleton<SemanticTokensHandler>();

        options.AddHandler<TextDocumentSyncHandler>()
               .AddHandler<WorkspaceFoldersHandler>()
               .AddHandler<WatchedFilesHandler>();

        options.OnStarted((languageServer, ct) =>
        {
            // Seed workspace scopes from the folders sent during the initialize handshake.
            var scopeManager = languageServer.Services.GetRequiredService<ILspWorkspaceScopeManager>();
            serverServices = languageServer.Services; // Caching the service provider for later use in handlers that don't have it injected.
            if (languageServer.ClientSettings.WorkspaceFolders != null)
            {
                foreach (var folder in languageServer.ClientSettings.WorkspaceFolders)
                {
                    var path = folder.Uri.GetFileSystemPath();
                    if (!string.IsNullOrEmpty(path))
                        scopeManager.OpenWorkspace(path);
                }
            }

            return Task.CompletedTask;
        });
        options.OnInitialized((languageServer, request, response, ct) =>
        {
            var tokenService = languageServer.Services.GetRequiredService<ISemanticTokenService>();

            response.Capabilities.SemanticTokensProvider = new SemanticTokensRegistrationOptions.StaticOptions
            {
                Legend = tokenService.Legend,
                Full = true,
                Range = false
            };

            return Task.CompletedTask;
        });

        // Bypassing registering the SemanticTokensHandler as a regular handler
        // Otherwise, it will register its capabilities dynamically, which VisualStudio doesn't support.
        // Directly wiring the handler to respond to specific request messages.
        options.OnRequest<SemanticTokensParams, SemanticTokens?>(
                    "textDocument/semanticTokens/full",
                    (request, ct) => serverServices!.GetRequiredService<SemanticTokensHandler>().Handle(request, ct));
        options.OnRequest<SemanticTokensDeltaParams, SemanticTokensFullOrDelta?>(
                    "textDocument/semanticTokens/full/delta",
                    (request, ct) => serverServices!.GetRequiredService<SemanticTokensHandler>().Handle(request, ct));

    }
}
