#nullable enable

using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SecureChat.Shared.DTOs.Group;

/// <summary>
/// Represents a request to create a group chat.
/// </summary>
public record CreateGroupDto
{
    /// <summary>Gets the group name.</summary>
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    /// <summary>Gets the initial member user identifiers.</summary>
    [JsonPropertyName("memberIds")]
    public IReadOnlyCollection<Guid> MemberIds { get; init; } = Array.Empty<Guid>();
}
