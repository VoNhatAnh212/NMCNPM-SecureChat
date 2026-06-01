#nullable enable

using System;
using System.Text.Json.Serialization;

namespace SecureChat.Shared.DTOs.Chat;

/// <summary>
/// Represents an encrypted chat session key for a device.
/// </summary>
public record SessionKeyDto
{
    /// <summary>Gets the chat identifier.</summary>
    [JsonPropertyName("chatId")]
    public Guid ChatId { get; init; }

    /// <summary>Gets the encrypted session key.</summary>
    [JsonPropertyName("encryptedSessionKey")]
    public string EncryptedSessionKey { get; init; } = string.Empty;

    /// <summary>Gets the target device identifier.</summary>
    [JsonPropertyName("deviceId")]
    public Guid DeviceId { get; init; }

    /// <summary>Gets the time when the session key was created.</summary>
    [JsonPropertyName("createdAt")]
    public DateTimeOffset CreatedAt { get; init; }
}
