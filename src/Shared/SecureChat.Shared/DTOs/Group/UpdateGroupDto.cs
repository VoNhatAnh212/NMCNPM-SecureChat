#nullable enable

using System.Text.Json.Serialization;

namespace SecureChat.Shared.DTOs.Group;

/// <summary>
/// Represents a request to update group metadata.
/// </summary>
public record UpdateGroupDto
{
    /// <summary>Gets the updated group name.</summary>
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    /// <summary>Gets the updated group avatar URL.</summary>
    [JsonPropertyName("avatarUrl")]
    public string AvatarUrl { get; init; } = string.Empty;
}
