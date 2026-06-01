#nullable enable

using System;
using System.Text.Json.Serialization;

namespace SecureChat.Shared.DTOs.User;

/// <summary>
/// Represents public key material for a user device.
/// </summary>
public record PublicKeyDto
{
    /// <summary>Gets the identifier of the user who owns the key.</summary>
    [JsonPropertyName("userId")]
    public Guid UserId { get; init; }

    /// <summary>Gets the identifier of the device that owns the key.</summary>
    [JsonPropertyName("deviceId")]
    public Guid DeviceId { get; init; }

    /// <summary>Gets the PEM encoded public key.</summary>
    [JsonPropertyName("publicKeyPem")]
    public string PublicKeyPem { get; init; } = string.Empty;

    /// <summary>Gets the time when the public key was created.</summary>
    [JsonPropertyName("createdAt")]
    public DateTimeOffset CreatedAt { get; init; }
}
