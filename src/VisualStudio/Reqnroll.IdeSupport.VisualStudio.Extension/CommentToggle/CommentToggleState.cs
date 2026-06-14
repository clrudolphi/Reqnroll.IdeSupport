#nullable enable

namespace Reqnroll.IdeSupport.VisualStudio.Extension.CommentToggle;

/// <summary>
/// Container-registered singleton holder for the runtime-created F13 Comment/Uncomment service.
/// </summary>
/// <remarks>
/// <see cref="CommentToggleService"/> depends on <c>LspInterceptingPipe</c>, which only exists after
/// the language server connection is established — too late for plain DI construction.
/// <see cref="ReqnrollLanguageClient"/> populates this on server init and clears it on dispose;
/// <see cref="CommentToggleCommand"/> reads it via constructor injection.
/// </remarks>
internal sealed class CommentToggleState
{
    /// <summary>Set once the server has initialised; null before that and after dispose.</summary>
    public CommentToggleService? Service { get; set; }
}
