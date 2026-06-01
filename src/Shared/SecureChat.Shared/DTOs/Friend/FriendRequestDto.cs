#nullable enable

using System;
using System.Text.Json.Serialization;
using SecureChat.Shared.DTOs.User;
using SecureChat.Shared.Enums;

namespace SecureChat.Shared.DTOs.Friend;

/// <summary>
/// Represents a friend request.
/// </summary>
public record FriendRequestDto
{
    /// <summary>Gets the friend request identifier.</summary>
    [JsonPropertyName("id")]
    public Guid Id { get; init; }

    /// <summary>Gets the user who sent the request.</summary>
    [JsonPropertyName("fromUser")]
    public UserDto FromUser { get; init; } = new();

    /// <summary>Gets the user who received the request.</summary>
    [JsonPropertyName("toUser")]
    public UserDto ToUser { get; init; } = new();

    /// <summary>Gets the target user identifier.</summary>
    [JsonPropertyName("toUserId")]
    public Guid ToUserId { get; init; }

    /// <summary>Gets the optional request message.</summary>
    [JsonPropertyName("message")]
    public string Message { get; init; } = string.Empty;

    /// <summary>Gets the time when the request was sent.</summary>
    [JsonPropertyName("sentAt")]
    public DateTimeOffset SentAt { get; init; }

    /// <summary>Gets the request status.</summary>
    [JsonPropertyName("status")]
    public FriendStatus Status { get; init; }
}
