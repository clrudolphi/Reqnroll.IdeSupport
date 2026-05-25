using OmniSharp.Extensions.LanguageServer.Protocol;
using Reqnroll.IdeSupport.Common.Diagnostics;
using Reqnroll.IdeSupport.LSP.Core.Discovery;
using Reqnroll.IdeSupport.LSP.Core.Editor.Services.Parsing.GherkinDocuments;
using Reqnroll.IdeSupport.LSP.Server.Workspace;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Reqnroll.IdeSupport.LSP.Server.Services;

public class GherkinDocumentTagsChangedEventArgs : EventArgs
{
    public DocumentUri Uri { get; }
    public int Version { get; }
    public IReadOnlyCollection<DeveroomTag> Tags { get; }
    public GherkinDocumentTagsChangedEventArgs(DocumentUri uri, int version, IReadOnlyCollection<DeveroomTag> tags)
    {
        Uri = uri;
        Version = version;
        Tags = tags;
    }
}
public class GherkinDocumentTaggerService : IGherkinDocumentTaggerService
{
    private readonly IDeveroomTagParser _tagParser;
    private readonly IBindingRegistryProvider _bindingRegistryProvider;
    private readonly ILspWorkspaceScopeManager _scopeManager;
    private readonly IDeveroomLogger _logger;
    private readonly IDocumentBufferService _documentBufferService;
    private ConcurrentDictionary<(DocumentUri, int), IReadOnlyCollection<DeveroomTag>> _documentTagsCache = new();

    public GherkinDocumentTaggerService(
        IDocumentBufferService documentBufferService,
        IDeveroomTagParser tagParser,
        IBindingRegistryProvider bindingRegistryProvider,
        ILspWorkspaceScopeManager scopeManager,
        IDeveroomLogger logger)
    {
        _documentBufferService = documentBufferService;
        _tagParser = tagParser;
        _bindingRegistryProvider = bindingRegistryProvider;
        _scopeManager = scopeManager;
        _logger = logger;
    }

    public event EventHandler<GherkinDocumentTagsChangedEventArgs> GherkinDocumentTagsChanged;

    public async Task OnDocumentChangedAsync(DocumentUri uri, int? version)
    {
        if (!_documentBufferService.TryGet(uri, out var buffer))
            return;

        var snapshot = buffer?.ToGherkinTextSnapshot();

        if (snapshot != null)
        {
            if (version.HasValue && snapshot.Version != version)
            {
                _logger.LogWarning($"Version mismatch for document {uri}: expected {version}, got {snapshot.Version}");
                return;
            }
            var configuration = _scopeManager.GetConfigurationProviderForUri(uri).GetConfiguration();
            var tags = _tagParser.Parse(snapshot);
            _documentTagsCache[(uri, snapshot.Version)] = tags;
            _logger.LogInfo($"Parsed {tags.Count} tags from document {uri}");
            GherkinDocumentTagsChanged?.Invoke(this, new GherkinDocumentTagsChangedEventArgs(uri, snapshot.Version, tags));
            PurgeTagsForVersionsPriorTo(uri, version);
        }
    }

    private void PurgeTagsForVersionsPriorTo(DocumentUri uri, int? version)
    {
        if (!version.HasValue)
            return;

        var keysToRemove = new List<(DocumentUri, int)>();
        foreach (var key in _documentTagsCache.Keys)
        {
            if (key.Item1 == uri && key.Item2 < version.Value)
                keysToRemove.Add(key);
        }
        foreach (var key in keysToRemove)
        {
            _documentTagsCache.TryRemove(key, out _);
            _logger.LogInfo($"Removed tags cache for document {uri} with version {key.Item2} due to new version {version}");
        }
    }

    public async Task OnDocumentClosedAsync(DocumentUri uri)
    {
        PurgeTagsForVersionsPriorTo(uri, int.MaxValue);
    }

    public async Task<IReadOnlyCollection<DeveroomTag>> GetTagsAsync(DocumentUri uri, int version)
    {
        if (_documentTagsCache.TryGetValue((uri, version), out var tags))
        {
            return tags;
        }

        // Cache miss – trigger an on-demand parse if the buffer is available.
        if (_documentBufferService.TryGet(uri, out var buffer) && buffer != null)
        {
            _logger.LogInfo($"Cache miss for {uri} v{version} – parsing on demand");
            await OnDocumentChangedAsync(uri, version).ConfigureAwait(false);
            if (_documentTagsCache.TryGetValue((uri, version), out tags))
                return tags;
        }

        _logger.LogWarning($"No tags found in cache for document {uri} with version {version}");
        return Array.Empty<DeveroomTag>();
    }
}
