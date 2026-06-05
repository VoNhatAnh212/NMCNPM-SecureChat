#nullable enable

using System.Text.Json.Serialization;

namespace SecureChat.Shared.DTOs.Auth;

/// <summary>
/// Represents the result of starting a password login on an untrusted device.
/// </summary>
public record PasswordLoginStartResponseDto
{
    /// <summary>Gets a value indicating whether an OTP is required to trust this device.</summary>
    [JsonPropertyName("requiresOtp")]
    public bool RequiresOtp { get; init; }

    /// <summary>Gets the destination email address for the OTP.</summary>
    [JsonPropertyName("email")]
    public string Email { get; init; } = string.Empty;

    /// <summary>Gets the account display name.</summary>
    [JsonPropertyName("displayName")]
    public string DisplayName { get; init; } = string.Empty;

    /// <summary>Gets a human-readable status message.</summary>
    [JsonPropertyName("message")]
    public string Message { get; init; } = string.Empty;
}
