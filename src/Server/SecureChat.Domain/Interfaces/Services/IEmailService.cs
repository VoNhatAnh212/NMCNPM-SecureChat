#nullable enable

namespace SecureChat.Domain.Interfaces.Services;

/// <summary>
/// Defines email delivery operations.
/// </summary>
public interface IEmailService
{
    /// <summary>Sends an OTP code to an email address.</summary>
    /// <param name="email">The destination email address.</param>
    /// <param name="otpCode">The OTP code.</param>
    /// <param name="displayName">The display name of the recipient.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task SendOtpAsync(string email, string otpCode, string displayName);

    /// <summary>Sends a security alert email.</summary>
    /// <param name="email">The destination email address.</param>
    /// <param name="eventType">The security event type.</param>
    /// <param name="deviceName">The device name.</param>
    /// <param name="ipAddress">The IP address associated with the event.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task SendSecurityAlertAsync(string email, string eventType, string deviceName, string ipAddress);
}
