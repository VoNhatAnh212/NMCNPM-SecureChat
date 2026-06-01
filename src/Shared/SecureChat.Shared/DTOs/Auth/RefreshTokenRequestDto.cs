#nullable enable

using System.Text.Json.Serialization;

namespace SecureChat.Shared.DTOs.Auth;

/// <summary>
/// Represents a request to refresh an access token.
/// </summary>
public record RefreshTokenRequestDto
{
    /// <summary>Gets the refresh token issued by the server.</summary>
    [JsonPropertyName("refreshToken")]
    public string RefreshToken { get; init; } = string.Empty;
}
