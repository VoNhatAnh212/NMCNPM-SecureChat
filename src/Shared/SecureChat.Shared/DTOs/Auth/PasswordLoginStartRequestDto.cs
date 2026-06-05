#nullable enable

using System.Text.Json.Serialization;

namespace SecureChat.Shared.DTOs.Auth;

/// <summary>
/// Represents a request to start login on a device that is not trusted locally yet.
/// </summary>
public record PasswordLoginStartRequestDto
{
    /// <summary>Gets the unique username. SecureChat currently uses the account email.</summary>
    [JsonPropertyName("username")]
    public string Username { get; init; } = string.Empty;

    /// <summary>Gets the client-side SHA-256 password hash.</summary>
    [JsonPropertyName("passwordHash")]
    public string PasswordHash { get; init; } = string.Empty;
}
