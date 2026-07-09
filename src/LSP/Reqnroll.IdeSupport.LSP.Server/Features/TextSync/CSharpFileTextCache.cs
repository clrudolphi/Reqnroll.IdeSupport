using OmniSharp.Extensions.LanguageServer.Protocol;

namespace Reqnroll.IdeSupport.LSP.Server.Features.TextSync;

public record CSharpFileText(DocumentUri Uri, string Text);

/// <summary>
/// Tracks the current (possibly unsaved) text of every <c>.cs</c> file the client has open,
/// independent of <see cref="IDocumentBufferService"/> — which is deliberately Gherkin-only and
/// never populated for <c>.cs</c> documents (see <c>TextDocumentSyncHandler</c>).
/// </summary>
/// <remarks>
/// Without this, any code that needs a <c>.cs</c> file's live content (e.g. F16 rename locating
/// the attribute literal to edit, or F2's incremental Roslyn reconciliation) had no source but
/// disk, which is stale for any unsaved edit — whether that edit came from the user typing
/// directly in the file or from this server's own F16 rename applying its own edit.
/// <c>TextDocumentSyncHandler</c> populates this on every <c>.cs</c> <c>didOpen</c>/
/// <c>didChange</c> regardless of source, and removes it on <c>didClose</c> — once closed, disk
/// is the source of truth again (VS prompts to save or discard before a close reaches the
/// server).
/// </remarks>
public interface ICSharpFileTextCache
{
    void Update(DocumentUri uri, string text);
    bool TryGet(DocumentUri uri, out string? text);
    void Remove(DocumentUri uri);
    IEnumerable<CSharpFileText> All { get; }
}

public sealed class CSharpFileTextCache : ICSharpFileTextCache
{
    private readonly ConcurrentDictionary<string, CSharpFileText> _texts = new(StringComparer.OrdinalIgnoreCase);

    public void Update(DocumentUri uri, string text)
        => _texts[uri.ToString()] = new CSharpFileText(uri, text);

    public bool TryGet(DocumentUri uri, out string? text)
    {
        if (_texts.TryGetValue(uri.ToString(), out var entry))
        {
            text = entry.Text;
            return true;
        }

        text = null;
        return false;
    }

    public void Remove(DocumentUri uri)
        => _texts.TryRemove(uri.ToString(), out _);

    public IEnumerable<CSharpFileText> All => _texts.Values;
}
