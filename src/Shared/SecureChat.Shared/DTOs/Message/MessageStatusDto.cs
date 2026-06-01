#nullable enable

using System;
using System.Text.Json.Serialization;
using SecureChat.Shared.Enums;

namespace SecureChat.Shared.DTOs.Message;

/// <summary>
/// Represents a message status update.
/// </summary>
public record MessageStatusDto
{
    /// <summary>Gets the message identifier.</summary>
    [JsonPropertyName("messageId")]
    public Guid MessageId { get; init; }

    /// <summary>Gets the new message status.</summary>
    [JsonPropertyName("status")]
    public MessageStatus Status { get; init; }

    /// <summary>Gets the time when the status was updated.</summary>
    [JsonPropertyName("updatedAt")]
    public DateTimeOffset UpdatedAt { get; init; }
}
