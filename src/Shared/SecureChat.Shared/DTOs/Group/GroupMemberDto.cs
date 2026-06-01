#nullable enable

using System;
using System.Text.Json.Serialization;
using SecureChat.Shared.Enums;

namespace SecureChat.Shared.DTOs.Group;

/// <summary>
/// Represents a member of a group chat.
/// </summary>
public record GroupMemberDto
{
    /// <summary>Gets the member user identifier.</summary>
    [JsonPropertyName("userId")]
    public Guid UserId { get; init; }

    /// <summary>Gets the member display name.</summary>
    [JsonPropertyName("displayName")]
    public string DisplayName { get; init; } = string.Empty;

    /// <summary>Gets the member avatar URL.</summary>
    [JsonPropertyName("avatarUrl")]
    public string AvatarUrl { get; init; } = string.Empty;

    /// <summary>Gets the member role.</summary>
    [JsonPropertyName("role")]
    public GroupRole Role { get; init; }

    /// <summary>Gets the time when the member joined the group.</summary>
    [JsonPropertyName("joinedAt")]
    public DateTimeOffset JoinedAt { get; init; }
}
