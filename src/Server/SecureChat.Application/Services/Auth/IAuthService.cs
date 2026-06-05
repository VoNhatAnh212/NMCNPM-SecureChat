#nullable enable

using SecureChat.Shared.DTOs.Auth;
using SecureChat.Shared.DTOs.Device;

namespace SecureChat.Application.Services.Auth;

/// <summary>
/// Defines authentication application operations.
/// </summary>
public interface IAuthService
{
    /// <summary>Registers an email address and sends an OTP.</summary>
    /// <param name="request">The registration request.</param>
    /// <returns>The registration response.</returns>
    Task<RegisterResponseDto> RegisterAsync(RegisterRequestDto request);

    /// <summary>Verifies a registration OTP.</summary>
    /// <param name="request">The OTP verification request.</param>
    /// <returns>True when verification succeeds.</returns>
    Task<bool> VerifyOtpAsync(OtpVerifyRequestDto request);

    /// <summary>Verifies a registration OTP, registers the first device, and issues tokens.</summary>
    /// <param name="request">The complete registration request.</param>
    /// <returns>The authentication response for the newly registered device.</returns>
    Task<LoginVerifyResponseDto> CompleteRegistrationAsync(CompleteRegistrationRequestDto request);

    /// <summary>Starts password login for a device that is not trusted locally yet.</summary>
    /// <param name="request">The password login request.</param>
    /// <returns>The login start response.</returns>
    Task<PasswordLoginStartResponseDto> StartPasswordLoginAsync(PasswordLoginStartRequestDto request);

    /// <summary>Completes password login by verifying OTP and registering a new trusted device.</summary>
    /// <param name="request">The password device login request.</param>
    /// <returns>The authentication response.</returns>
    Task<LoginVerifyResponseDto> CompletePasswordDeviceLoginAsync(CompletePasswordDeviceLoginRequestDto request);

    /// <summary>Gets a login challenge after verifying the password for an existing trusted device.</summary>
    /// <param name="request">The password challenge request.</param>
    /// <returns>The login challenge response.</returns>
    Task<LoginChallengeResponseDto> GetPasswordLoginChallengeAsync(PasswordLoginChallengeRequestDto request);

    /// <summary>Gets a login challenge for a trusted device.</summary>
    /// <param name="request">The challenge request.</param>
    /// <returns>The login challenge response.</returns>
    Task<LoginChallengeResponseDto> GetLoginChallengeAsync(LoginChallengeRequestDto request);

    /// <summary>Verifies a login challenge signature.</summary>
    /// <param name="request">The login verification request.</param>
    /// <returns>The authentication response.</returns>
    Task<LoginVerifyResponseDto> VerifyLoginAsync(LoginVerifyRequestDto request);

    /// <summary>Refreshes an access token.</summary>
    /// <param name="request">The refresh token request.</param>
    /// <returns>The authentication response.</returns>
    Task<LoginVerifyResponseDto> RefreshTokenAsync(RefreshTokenRequestDto request);

    /// <summary>Logs a device out by revoking refresh tokens.</summary>
    /// <param name="deviceId">The device identifier.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task LogoutAsync(Guid deviceId);

    /// <summary>Logs all devices out by revoking all refresh tokens for a user.</summary>
    /// <param name="userId">The user identifier.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task LogoutAllDevicesAsync(Guid userId);

    /// <summary>Registers a new trusted device for a user.</summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="request">The device registration request.</param>
    /// <returns>The registered device.</returns>
    Task<DeviceDto> RegisterDeviceAsync(Guid userId, RegisterDeviceDto request);

    /// <summary>Rotates the public key for the current device.</summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="deviceId">The current device identifier.</param>
    /// <param name="request">The new key material.</param>
    /// <returns>The updated device.</returns>
    Task<DeviceDto> RotateCurrentDeviceKeyAsync(Guid userId, Guid deviceId, RegisterDeviceDto request);
}
