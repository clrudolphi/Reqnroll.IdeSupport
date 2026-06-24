using MediatR;
using OmniSharp.Extensions.LanguageServer.Protocol;

namespace Reqnroll.IdeSupport.LSP.Server.Pipeline;

/// <summary>
/// Published after a feature document has been (re)parsed and its binding matches recomputed
/// and stored in <see cref="Core.Matching.IBindingMatchService"/> — i.e. whenever the match
/// cache for <see cref="Uri"/> changes, whether triggered by a text edit, a binding-registry
/// replacement, or a configuration change.
/// </summary>
/// <remarks>
/// This is the <c>MatchCacheChangedNotification</c> of section 3 / section 6 of the LSP IDE
/// Support design. Consumers re-read the current tags / match set rather than receiving them
/// <see cref="Pipeline.SemanticTokensRefreshHandler"/>
/// asks the client to refresh semantic tokens, and the (future) diagnostics aggregator pushes
/// <c>textDocument/publishDiagnostics</c>.
/// </remarks>
public record MatchCacheChangedNotification(
    DocumentUri Uri,
    int Version) : INotification;
