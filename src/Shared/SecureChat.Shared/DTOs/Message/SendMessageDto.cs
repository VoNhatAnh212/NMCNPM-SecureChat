#nullable enable

using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using SecureChat.Shared.Enums;

namespace SecureChat.Shared.DTOs.Message;

/// <summary>
/// Represents a request to send an encrypted message.
/// </summary>
public record SendMessageDto
{
    /// <summary>Gets the destination chat identifier.</summary>
    [JsonPropertyName("chatId")]
    public Guid ChatId { get; init; }

    /// <summary>Gets the encrypted message content.</summary>
    [JsonPropertyName("encryptedContent")]
    public string EncryptedContent { get; init; } = string.Empty;

    /// <summary>Gets the message payload type.</summary>
    [JsonPropertyName("messageType")]
    public MessageType MessageType { get; init; }

    /// <summary>Gets the encrypted session key used for this message.</summary>
    [JsonPropertyName("encryptedSessionKey")]
    public string EncryptedSessionKey { get; init; } = string.Empty;

    /// <summary>Gets the initialization vector.</summary>
    [JsonPropertyName("iv")]
    public string IV { get; init; } = string.Empty;

    /// <summary>Gets the authentication tag.</summary>
    [JsonPropertyName("authTag")]
    public string AuthTag { get; init; } = string.Empty;

    /// <summary>Gets the identifiers of attachments included in the message.</summary>
    [JsonPropertyName("attachmentIds")]
    public IReadOnlyCollection<Guid> AttachmentIds { get; init; } = Array.Empty<Guid>();
}
