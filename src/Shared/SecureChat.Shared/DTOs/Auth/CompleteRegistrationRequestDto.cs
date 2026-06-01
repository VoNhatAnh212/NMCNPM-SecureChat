#nullable enable

using System.Text.Json.Serialization;
using SecureChat.Shared.DTOs.Device;

namespace SecureChat.Shared.DTOs.Auth;

/// <summary>
/// Represents the final registration step: OTP verification plus first device registration.
/// </summary>
public record CompleteRegistrationRequestDto
{
    /// <summary>Gets the email address being verified.</summary>
    [JsonPropertyName("email")]
    public string Email { get; init; } = string.Empty;

    /// <summary>Gets the OTP code sent to the email address.</summary>
    [JsonPropertyName("otpCode")]
    public string OtpCode { get; init; } = string.Empty;

    /// <summary>Gets the trusted device registration payload.</summary>
    [JsonPropertyName("device")]
    public RegisterDeviceDto Device { get; init; } = new();
}
