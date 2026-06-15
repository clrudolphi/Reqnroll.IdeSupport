#nullable enable

using System;
using System.Collections.Concurrent;

namespace Reqnroll.IdeSupport.LSP.Core.Rename;

/// <summary>
/// Tracks pending rename sessions (multi-attribute picker flow).
/// A session is established when the client calls reqnroll/selectRenameTarget
/// and consumed (or expired) on the subsequent textDocument/rename.
/// </summary>
public class RenameSessionManager
{
    private static readonly TimeSpan SessionDuration = TimeSpan.FromSeconds(30);
    
    // Key: (uri, documentVersion) → (attributeIndex, expiresAt)
    private readonly ConcurrentDictionary<(string Uri, int Version), (int AttributeIndex, DateTime ExpiresAt)> _sessions = new();

    /// <summary>Stores a pending rename session with a 30-second expiry.</summary>
    public void SetSession(string uri, int version, int attributeIndex)
    {
        var key = (uri, version);
        _sessions[key] = (attributeIndex, DateTime.UtcNow + SessionDuration);
        Cleanup();
    }

    /// <summary>
    /// Attempts to consume a pending session. Returns true if a valid (non-expired)
    /// session exists for the given URI+version, and outputs the attributeIndex.
    /// The session is removed on successful consumption.
    /// </summary>
    public bool TryConsume(string uri, int version, out int attributeIndex)
    {
        attributeIndex = 0;
        Cleanup();
        
        var key = (uri, version);
        if (_sessions.TryRemove(key, out var entry))
        {
            if (entry.ExpiresAt > DateTime.UtcNow)
            {
                attributeIndex = entry.AttributeIndex;
                return true;
            }
        }
        return false;
    }

    /// <summary>Removes expired sessions on a best-effort basis.</summary>
    private void Cleanup()
    {
        var now = DateTime.UtcNow;
        foreach (var kvp in _sessions)
        {
            if (kvp.Value.ExpiresAt <= now)
                _sessions.TryRemove(kvp.Key, out _);
        }
    }
}
