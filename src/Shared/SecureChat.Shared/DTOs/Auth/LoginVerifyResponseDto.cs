#nullable enable

using System;
using System.Text.Json.Serialization;
using SecureChat.Shared.DTOs.User;

namespace SecureChat.Shared.DTOs.Auth;

/// <summary>
/// Represents the authentication result returned after challenge verification.
/// </summary>
public record LoginVerifyResponseDto
{
    /// <summary>Gets the short-lived access token.</summary>
    [JsonPropertyName("accessToken")]
    public string AccessToken { get; init; } = string.Empty;

    /// <summary>Gets the refresh token.</summary>
    [JsonPropertyName("refreshToken")]
    public string RefreshToken { get; init; } = string.Empty;

    /// <summary>Gets the time when the access token expires.</summary>
    [JsonPropertyName("expiresAt")]
    public DateTimeOffset ExpiresAt { get; init; }

    /// <summary>Gets the authenticated user.</summary>
    [JsonPropertyName("user")]
    public UserDto User { get; init; } = new();
}
