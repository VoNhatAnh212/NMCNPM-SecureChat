#nullable enable

using System;
using System.Text.Json.Serialization;

namespace SecureChat.Shared.DTOs.Auth;

/// <summary>
/// Represents a request for a cryptographic login challenge.
/// </summary>
public record LoginChallengeRequestDto
{
    /// <summary>Gets the requesting device identifier.</summary>
    [JsonPropertyName("deviceId")]
    public Guid DeviceId { get; init; }
}
