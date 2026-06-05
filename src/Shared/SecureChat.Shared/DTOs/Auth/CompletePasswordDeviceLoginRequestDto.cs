#nullable enable

using System.Text.Json.Serialization;
using SecureChat.Shared.DTOs.Device;

namespace SecureChat.Shared.DTOs.Auth;

/// <summary>
/// Represents the final password login step for trusting a new device.
/// </summary>
public record CompletePasswordDeviceLoginRequestDto
{
    /// <summary>Gets the unique username. SecureChat currently uses the account email.</summary>
    [JsonPropertyName("username")]
    public string Username { get; init; } = string.Empty;

    /// <summary>Gets the client-side SHA-256 password hash.</summary>
    [JsonPropertyName("passwordHash")]
    public string PasswordHash { get; init; } = string.Empty;

    /// <summary>Gets the OTP code sent to the account email.</summary>
    [JsonPropertyName("otpCode")]
    public string OtpCode { get; init; } = string.Empty;

    /// <summary>Gets the new trusted device registration payload.</summary>
    [JsonPropertyName("device")]
    public RegisterDeviceDto Device { get; init; } = new();
}
