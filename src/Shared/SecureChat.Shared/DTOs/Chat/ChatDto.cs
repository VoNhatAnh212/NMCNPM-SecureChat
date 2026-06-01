#nullable enable

using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using SecureChat.Shared.DTOs.Message;
using SecureChat.Shared.DTOs.User;
using SecureChat.Shared.Enums;

namespace SecureChat.Shared.DTOs.Chat;

/// <summary>
/// Represents a chat with members and the latest message.
/// </summary>
public record ChatDto
{
    /// <summary>Gets the chat identifier.</summary>
    [JsonPropertyName("id")]
    public Guid Id { get; init; }

    /// <summary>Gets the chat type.</summary>
    [JsonPropertyName("type")]
    public ChatType Type { get; init; }

    /// <summary>Gets the chat display name.</summary>
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    /// <summary>Gets the backing group identifier for group chats.</summary>
    [JsonPropertyName("groupId")]
    public Guid? GroupId { get; init; }

    /// <summary>Gets the chat avatar URL.</summary>
    [JsonPropertyName("avatarUrl")]
    public string AvatarUrl { get; init; } = string.Empty;

    /// <summary>Gets the chat members.</summary>
    [JsonPropertyName("members")]
    public IReadOnlyCollection<UserDto> Members { get; init; } = Array.Empty<UserDto>();

    /// <summary>Gets the latest message in the chat.</summary>
    [JsonPropertyName("lastMessage")]
    public MessageDto? LastMessage { get; init; }

    /// <summary>Gets the number of unread messages.</summary>
    [JsonPropertyName("unreadCount")]
    public int UnreadCount { get; init; }
}
