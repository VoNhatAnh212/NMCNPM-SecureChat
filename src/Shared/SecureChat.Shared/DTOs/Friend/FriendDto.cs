#nullable enable

using System;
using System.Text.Json.Serialization;

namespace SecureChat.Shared.DTOs.Friend;

/// <summary>
/// Represents a friend entry.
/// </summary>
public record FriendDto
{
    /// <summary>Gets the friend's user identifier.</summary>
    [JsonPropertyName("userId")]
    public Guid UserId { get; init; }

    /// <summary>Gets the friend's display name.</summary>
    [JsonPropertyName("displayName")]
    public string DisplayName { get; init; } = string.Empty;

    /// <summary>Gets the friend's avatar URL.</summary>
    [JsonPropertyName("avatarUrl")]
    public string AvatarUrl { get; init; } = string.Empty;

    /// <summary>Gets a value indicating whether the friend is online.</summary>
    [JsonPropertyName("isOnline")]
    public bool IsOnline { get; init; }

    /// <summary>Gets the friendship identifier.</summary>
    [JsonPropertyName("friendshipId")]
    public Guid FriendshipId { get; init; }
}
