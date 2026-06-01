#nullable enable

using System.Text.Json.Serialization;

namespace SecureChat.Shared.DTOs.Auth;

/// <summary>
/// Represents a request to verify an email one-time passcode.
/// </summary>
public record OtpVerifyRequestDto
{
    /// <summary>Gets the email address associated with the passcode.</summary>
    [JsonPropertyName("email")]
    public string Email { get; init; } = string.Empty;

    /// <summary>Gets the one-time passcode to verify.</summary>
    [JsonPropertyName("otpCode")]
    public string OtpCode { get; init; } = string.Empty;
}
