#nullable enable

using System;
using System.Text.Json.Serialization;

namespace SecureChat.Shared.DTOs.Chat;

/// <summary>
/// Represents a compact chat summary for chat lists.
/// </summary>
public record ChatSummaryDto
{
    /// <summary>Gets the chat identifier.</summary>
    [JsonPropertyName("id")]
    public Guid Id { get; init; }

    /// <summary>Gets the chat display name.</summary>
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    /// <summary>Gets the backing group identifier for group chats.</summary>
    [JsonPropertyName("groupId")]
    public Guid? GroupId { get; init; }

    /// <summary>Gets the chat avatar URL.</summary>
    [JsonPropertyName("avatarUrl")]
    public string AvatarUrl { get; init; } = string.Empty;

    /// <summary>Gets the preview text for the latest message.</summary>
    [JsonPropertyName("lastMessagePreview")]
    public string LastMessagePreview { get; init; } = string.Empty;

    /// <summary>Gets the time of the latest message.</summary>
    [JsonPropertyName("lastMessageAt")]
    public DateTimeOffset? LastMessageAt { get; init; }

    /// <summary>Gets the unread message count.</summary>
    [JsonPropertyName("unreadCount")]
    public int UnreadCount { get; init; }

    /// <summary>Gets a value indicating whether the chat is a group chat.</summary>
    [JsonPropertyName("isGroup")]
    public bool IsGroup { get; init; }
}
