#nullable enable

using System;
using System.Text.Json.Serialization;

namespace SecureChat.Shared.DTOs.User;

/// <summary>
/// Represents public user information.
/// </summary>
public record UserDto
{
    /// <summary>Gets the user identifier.</summary>
    [JsonPropertyName("id")]
    public Guid Id { get; init; }

    /// <summary>Gets the user's display name.</summary>
    [JsonPropertyName("displayName")]
    public string DisplayName { get; init; } = string.Empty;

    /// <summary>Gets the user's email address.</summary>
    [JsonPropertyName("email")]
    public string Email { get; init; } = string.Empty;

    /// <summary>Gets the user's avatar URL.</summary>
    [JsonPropertyName("avatarUrl")]
    public string AvatarUrl { get; init; } = string.Empty;

    /// <summary>Gets a value indicating whether the user is online.</summary>
    [JsonPropertyName("isOnline")]
    public bool IsOnline { get; init; }

    /// <summary>Gets the last time the user was seen online.</summary>
    [JsonPropertyName("lastSeen")]
    public DateTimeOffset? LastSeen { get; init; }
}
