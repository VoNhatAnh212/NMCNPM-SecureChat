#nullable enable

using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using SecureChat.Shared.DTOs.File;
using SecureChat.Shared.Enums;

namespace SecureChat.Shared.DTOs.Message;

/// <summary>
/// Represents a message returned by the messaging API.
/// </summary>
public record MessageDto
{
    /// <summary>Gets the message identifier.</summary>
    [JsonPropertyName("id")]
    public Guid Id { get; init; }

    /// <summary>Gets the chat identifier.</summary>
    [JsonPropertyName("chatId")]
    public Guid ChatId { get; init; }

    /// <summary>Gets the sender user identifier.</summary>
    [JsonPropertyName("senderId")]
    public Guid SenderId { get; init; }

    /// <summary>Gets the sender display name.</summary>
    [JsonPropertyName("senderName")]
    public string SenderName { get; init; } = string.Empty;

    /// <summary>Gets the encrypted message content.</summary>
    [JsonPropertyName("encryptedContent")]
    public string EncryptedContent { get; init; } = string.Empty;

    /// <summary>Gets the initialization vector.</summary>
    [JsonPropertyName("iv")]
    public string IV { get; init; } = string.Empty;

    /// <summary>Gets the authentication tag.</summary>
    [JsonPropertyName("authTag")]
    public string AuthTag { get; init; } = string.Empty;

    /// <summary>Gets the message payload type.</summary>
    [JsonPropertyName("messageType")]
    public MessageType MessageType { get; init; }

    /// <summary>Gets the delivery status.</summary>
    [JsonPropertyName("status")]
    public MessageStatus Status { get; init; }

    /// <summary>Gets the time when the message was sent.</summary>
    [JsonPropertyName("sentAt")]
    public DateTimeOffset SentAt { get; init; }

    /// <summary>Gets the encrypted attachments associated with the message.</summary>
    [JsonPropertyName("attachments")]
    public IReadOnlyCollection<AttachmentDto> Attachments { get; init; } = Array.Empty<AttachmentDto>();
}
