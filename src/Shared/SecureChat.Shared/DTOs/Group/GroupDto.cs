#nullable enable

using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SecureChat.Shared.DTOs.Group;

/// <summary>
/// Represents a group chat.
/// </summary>
public record GroupDto
{
    /// <summary>Gets the group identifier.</summary>
    [JsonPropertyName("id")]
    public Guid Id { get; init; }

    /// <summary>Gets the group name.</summary>
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    /// <summary>Gets the group avatar URL.</summary>
    [JsonPropertyName("avatarUrl")]
    public string AvatarUrl { get; init; } = string.Empty;

    /// <summary>Gets the creator user identifier.</summary>
    [JsonPropertyName("creatorId")]
    public Guid CreatorId { get; init; }

    /// <summary>Gets the number of group members.</summary>
    [JsonPropertyName("memberCount")]
    public int MemberCount { get; init; }

    /// <summary>Gets the time when the group was created.</summary>
    [JsonPropertyName("createdAt")]
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>Gets the group members.</summary>
    [JsonPropertyName("members")]
    public IReadOnlyCollection<GroupMemberDto> Members { get; init; } = Array.Empty<GroupMemberDto>();
}
