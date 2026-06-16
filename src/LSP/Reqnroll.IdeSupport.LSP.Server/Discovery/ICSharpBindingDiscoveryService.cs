using OmniSharp.Extensions.LanguageServer.Protocol;

namespace Reqnroll.IdeSupport.LSP.Server.Discovery;

/// <summary>
/// Applies an immediate, source-level (Roslyn) binding update for a single <c>.cs</c> file,
/// patching the owning project's binding registry so step matches update as the user types —
/// before any build. See <see cref="CSharpBindingDiscoveryService"/> and design doc feature F2.
/// </summary>
public interface ICSharpBindingDiscoveryService
{
    /// <summary>
    /// Re-discovers the bindings declared in <paramref name="text"/> (the current contents of the
    /// <c>.cs</c> document at <paramref name="uri"/>) and replaces that file's entries in its
    /// project's binding registry. No-ops when the document has no owning project or the project
    /// has no binding provider yet.
    /// </summary>
    /// <param name="isOpen"><see langword="true"/> when triggered by a <c>textDocument/didOpen</c>
    /// event; <see langword="false"/> when triggered by <c>textDocument/didChange</c>. Used for
    /// telemetry to distinguish <c>csOpen</c> from <c>csEdit</c> trigger contexts.</param>
    Task UpdateFromSourceAsync(DocumentUri uri, string text, bool isOpen, CancellationToken cancellationToken);
}
