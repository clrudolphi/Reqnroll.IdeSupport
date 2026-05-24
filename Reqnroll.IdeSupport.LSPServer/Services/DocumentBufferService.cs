using OmniSharp.Extensions.LanguageServer.Protocol;
using System.Collections.Concurrent;

namespace Reqnroll.IdeSupport.LSPServer.Services;

public record DocumentBuffer(DocumentUri Uri, int? Version, string Text);

public interface IDocumentBufferService
{
    void Update(DocumentUri uri, int? version, string text);
    void Remove(DocumentUri uri);
    bool TryGet(DocumentUri uri, out DocumentBuffer? buffer);
    IEnumerable<DocumentBuffer> All { get; }
}

public class DocumentBufferService : IDocumentBufferService
{
    private readonly ConcurrentDictionary<string, DocumentBuffer> _buffers = new();

    public void Update(DocumentUri uri, int? version, string text)
        => _buffers[uri.ToString()] = new DocumentBuffer(uri, version, text);

    public bool TryGet(DocumentUri uri, out DocumentBuffer? buffer)
        => _buffers.TryGetValue(uri.ToString(), out buffer);

    public void Remove(DocumentUri uri)
        => _buffers.TryRemove(uri.ToString(), out _);

    public IEnumerable<DocumentBuffer> All => _buffers.Values;
}