using OmniSharp.Extensions.LanguageServer.Protocol;
using Reqnroll.IdeSupport.LSP.Core.Editor.Services.Parsing.GherkinDocuments;

namespace Reqnroll.IdeSupport.LSP.Server.Services;

public record DocumentBuffer(DocumentUri Uri, int? Version, string Text, IReadOnlyCollection<DeveroomTag>? Tags = null);

public interface IDocumentBufferService
{
    void Update(DocumentUri uri, int? version, string text);
    void UpdateTags(DocumentUri uri, IReadOnlyCollection<DeveroomTag> tags);
    void Remove(DocumentUri uri);
    bool TryGet(DocumentUri uri, out DocumentBuffer? buffer);
    IEnumerable<DocumentBuffer> All { get; }
}

public class DocumentBufferService : IDocumentBufferService
{
    private readonly ConcurrentDictionary<string, DocumentBuffer> _buffers = new();

    private static string NormalizeKey(DocumentUri uri)
    {
        var key = uri.ToString();
        // Normalize drive letter (Windows): file:///C:/path → file:///c:/path
        if (key.StartsWith("file:///", StringComparison.OrdinalIgnoreCase) && key.Length > 8 && key[8] == ':')
            key = "file:///" + char.ToLowerInvariant(key[8]) + key.Substring(9);
        return key;
    }

    public void Update(DocumentUri uri, int? version, string text)
        => _buffers[NormalizeKey(uri)] = new DocumentBuffer(uri, version, text);

    public void UpdateTags(DocumentUri uri, IReadOnlyCollection<DeveroomTag> tags)
        => _buffers.AddOrUpdate(
            NormalizeKey(uri),
            _ => new DocumentBuffer(uri, null, string.Empty, tags),
            (_, existing) => existing with { Tags = tags });

    public bool TryGet(DocumentUri uri, out DocumentBuffer? buffer)
        => _buffers.TryGetValue(NormalizeKey(uri), out buffer);

    public void Remove(DocumentUri uri)
        => _buffers.TryRemove(NormalizeKey(uri), out _);

    public IEnumerable<DocumentBuffer> All => _buffers.Values;
}