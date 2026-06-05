#nullable enable

using System;
using System.Text.Json.Serialization;

namespace SecureChat.Shared.DTOs.Auth;

/// <summary>
/// Represents a password-gated request for a trusted-device login challenge.
/// </summary>
public record PasswordLoginChallengeRequestDto
{
    /// <summary>Gets the unique username. SecureChat currently uses the account email.</summary>
    [JsonPropertyName("username")]
    public string Username { get; init; } = string.Empty;

    /// <summary>Gets the client-side SHA-256 password hash.</summary>
    [JsonPropertyName("passwordHash")]
    public string PasswordHash { get; init; } = string.Empty;

    /// <summary>Gets the trusted device identifier.</summary>
    [JsonPropertyName("deviceId")]
    public Guid DeviceId { get; init; }
}
