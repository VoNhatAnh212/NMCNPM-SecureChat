#nullable enable

using System.Collections.Concurrent;

namespace SecureChat.Client.Services.Storage;

/// <summary>
/// Provides an in-memory thread-safe cache for chat and group session keys.
/// </summary>
public class SessionCache
{
    private readonly ConcurrentDictionary<string, byte[]> _sessionKeys = new();
    private readonly ConcurrentDictionary<string, byte[]> _groupKeys = new();

    /// <summary>Gets a session key for a chat.</summary>
    public byte[]? GetSessionKey(string chatId)
    {
        return _sessionKeys.TryGetValue(chatId, out var key) ? key : null;
    }

    /// <summary>Sets a session key for a chat.</summary>
    public void SetSessionKey(string chatId, byte[] key)
    {
        _sessionKeys[chatId] = key;
    }

    /// <summary>Removes a cached session key for a chat.</summary>
    public void RemoveSessionKey(string chatId)
    {
        _sessionKeys.TryRemove(chatId, out _);
    }

    /// <summary>Gets a group key by group and version.</summary>
    public byte[]? GetGroupKey(string groupId, int version)
    {
        return _groupKeys.TryGetValue(GetGroupKeyCacheKey(groupId, version), out var key) ? key : null;
    }

    /// <summary>Sets a group key by group and version.</summary>
    public void SetGroupKey(string groupId, int version, byte[] key)
    {
        _groupKeys[GetGroupKeyCacheKey(groupId, version)] = key;
    }

    /// <summary>Clears all cached keys.</summary>
    public void ClearAll()
    {
        _sessionKeys.Clear();
        _groupKeys.Clear();
    }

    private static string GetGroupKeyCacheKey(string groupId, int version)
    {
        return $"{groupId}:{version}";
    }
}
