#nullable enable

using SecureChat.Domain.Entities;

namespace SecureChat.Domain.Interfaces.Repositories;

/// <summary>
/// Defines public key persistence operations.
/// </summary>
public interface IPublicKeyRepository : IRepository<PublicKey>
{
    /// <summary>Gets the active public key for a device.</summary>
    /// <param name="deviceId">The device identifier.</param>
    /// <returns>The active public key when found; otherwise, null.</returns>
    Task<PublicKey?> GetActiveByDeviceIdAsync(Guid deviceId);

    /// <summary>Gets public keys owned by a user.</summary>
    /// <param name="userId">The user identifier.</param>
    /// <returns>The user's public keys.</returns>
    Task<IReadOnlyCollection<PublicKey>> GetByUserIdAsync(Guid userId);
}
