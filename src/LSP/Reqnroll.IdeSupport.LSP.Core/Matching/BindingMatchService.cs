#nullable enable

using System.Collections.Concurrent;
using Reqnroll.IdeSupport.LSP.Core.Discovery;

namespace Reqnroll.IdeSupport.LSP.Core.Matching;

/// <inheritdoc cref="IBindingMatchService"/>
public sealed class BindingMatchService : IBindingMatchService
{
    private readonly ConcurrentDictionary<string, FeatureBindingMatchSet> _cache = new();

    public void Store(FeatureBindingMatchSet matchSet)
    {
        if (matchSet == null) throw new ArgumentNullException(nameof(matchSet));
        _cache[matchSet.DocumentId] = matchSet;
    }

    public bool TryGet(string documentId, out FeatureBindingMatchSet matchSet)
    {
        if (documentId != null && _cache.TryGetValue(documentId, out var found))
        {
            matchSet = found;
            return true;
        }

        matchSet = FeatureBindingMatchSet.Empty;
        return false;
    }

    public void Invalidate(string documentId)
    {
        if (documentId != null)
            _cache.TryRemove(documentId, out _);
    }

    public void InvalidateAll() => _cache.Clear();

    public IReadOnlyList<StepBindingMatch> FindUsages(SourceLocation bindingLocation)
    {
        if (bindingLocation == null)
            return Array.Empty<StepBindingMatch>();

        var usages = new List<StepBindingMatch>();

        // Snapshot of values; ConcurrentDictionary enumeration is safe under concurrent writes.
        foreach (var set in _cache.Values)
            foreach (var step in set.Steps)
                if (step.BindingLocations.Any(loc => SameLocation(loc, bindingLocation)))
                    usages.Add(step);

        return usages;
    }

    private static bool SameLocation(SourceLocation a, SourceLocation b)
        => string.Equals(a.SourceFile, b.SourceFile, StringComparison.OrdinalIgnoreCase)
           && a.SourceFileLine == b.SourceFileLine;
}
