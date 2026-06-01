#nullable enable

using System;
using System.Text.Json.Serialization;

namespace SecureChat.Shared.DTOs.Auth;

/// <summary>
/// Represents a login challenge issued for device authentication.
/// </summary>
public record LoginChallengeResponseDto
{
    /// <summary>Gets the challenge that must be signed by the device.</summary>
    [JsonPropertyName("challenge")]
    public string Challenge { get; init; } = string.Empty;

    /// <summary>Gets the time when the challenge expires.</summary>
    [JsonPropertyName("expiresAt")]
    public DateTimeOffset ExpiresAt { get; init; }
}
