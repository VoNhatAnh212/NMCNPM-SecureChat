#nullable enable

using SecureChat.Domain.Entities;

namespace SecureChat.Domain.Interfaces.Repositories;

/// <summary>
/// Defines OTP persistence operations.
/// </summary>
public interface IOtpRepository : IRepository<OtpRequest>
{
    /// <summary>Gets a valid OTP request for an email and code.</summary>
    /// <param name="email">The email address.</param>
    /// <param name="otpCode">The OTP code.</param>
    /// <returns>The valid OTP request when found; otherwise, null.</returns>
    Task<OtpRequest?> GetValidOtpAsync(string email, string otpCode);

    /// <summary>Increments the attempt counter for an OTP request.</summary>
    /// <param name="id">The OTP request identifier.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task IncrementAttemptsAsync(Guid id);
}
