#nullable enable

using System.Text.Json.Serialization;

namespace SecureChat.Shared.DTOs.User;

/// <summary>
/// Represents detailed profile information for a user.
/// </summary>
public record UserProfileDto : UserDto
{
    /// <summary>Gets the fingerprint of the user's public key material.</summary>
    [JsonPropertyName("publicKeyFingerprint")]
    public string PublicKeyFingerprint { get; init; } = string.Empty;

    /// <summary>Gets the number of registered devices for the user.</summary>
    [JsonPropertyName("deviceCount")]
    public int DeviceCount { get; init; }
}
