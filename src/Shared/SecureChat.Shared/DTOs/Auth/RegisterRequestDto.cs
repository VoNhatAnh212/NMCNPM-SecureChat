#nullable enable

using System.Text.Json.Serialization;

namespace SecureChat.Shared.DTOs.Auth;

/// <summary>
/// Represents the payload used to register a new user.
/// </summary>
public record RegisterRequestDto
{
    /// <summary>Gets the user's email address.</summary>
    [JsonPropertyName("email")]
    public string Email { get; init; } = string.Empty;

    /// <summary>Gets the display name shown to other users.</summary>
    [JsonPropertyName("displayName")]
    public string DisplayName { get; init; } = string.Empty;

    /// <summary>Gets the account password used by password-based clients.</summary>
    [JsonPropertyName("password")]
    public string Password { get; init; } = string.Empty;
}
