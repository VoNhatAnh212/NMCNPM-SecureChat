#nullable enable

using SecureChat.Domain.Entities;

namespace SecureChat.Domain.Interfaces.Repositories;

/// <summary>
/// Defines refresh token persistence operations.
/// </summary>
public interface IRefreshTokenRepository : IRepository<RefreshToken>
{
    /// <summary>Gets an active refresh token.</summary>
    /// <param name="token">The refresh token value.</param>
    /// <returns>The active refresh token when found; otherwise, null.</returns>
    Task<RefreshToken?> GetActiveTokenAsync(string token);

    /// <summary>Revokes all refresh tokens for a device.</summary>
    /// <param name="deviceId">The device identifier.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task RevokeAllForDeviceAsync(Guid deviceId);
}
