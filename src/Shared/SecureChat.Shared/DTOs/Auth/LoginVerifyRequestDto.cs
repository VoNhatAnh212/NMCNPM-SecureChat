#nullable enable

using System;
using System.Text.Json.Serialization;

namespace SecureChat.Shared.DTOs.Auth;

/// <summary>
/// Represents a request to verify a signed login challenge.
/// </summary>
public record LoginVerifyRequestDto
{
    /// <summary>Gets the device identifier that signed the challenge.</summary>
    [JsonPropertyName("deviceId")]
    public Guid DeviceId { get; init; }

    /// <summary>Gets the signed challenge produced by the device private key.</summary>
    [JsonPropertyName("signedChallenge")]
    public string SignedChallenge { get; init; } = string.Empty;
}
