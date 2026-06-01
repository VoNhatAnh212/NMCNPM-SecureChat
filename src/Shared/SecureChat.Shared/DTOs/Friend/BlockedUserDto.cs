#nullable enable

using System;
using System.Text.Json.Serialization;

namespace SecureChat.Shared.DTOs.Friend;

/// <summary>
/// Represents a blocked user.
/// </summary>
public record BlockedUserDto
{
    /// <summary>Gets the blocked user identifier.</summary>
    [JsonPropertyName("userId")]
    public Guid UserId { get; init; }

    /// <summary>Gets the blocked user's display name.</summary>
    [JsonPropertyName("displayName")]
    public string DisplayName { get; init; } = string.Empty;

    /// <summary>Gets the time when the user was blocked.</summary>
    [JsonPropertyName("blockedAt")]
    public DateTimeOffset BlockedAt { get; init; }
}
