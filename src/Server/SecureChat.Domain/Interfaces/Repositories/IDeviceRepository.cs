#nullable enable

using SecureChat.Domain.Entities;

namespace SecureChat.Domain.Interfaces.Repositories;

/// <summary>
/// Defines device persistence operations.
/// </summary>
public interface IDeviceRepository : IRepository<Device>
{
    /// <summary>Gets devices owned by a user.</summary>
    /// <param name="userId">The user identifier.</param>
    /// <returns>The user's devices.</returns>
    Task<IReadOnlyCollection<Device>> GetByUserIdAsync(Guid userId);

    /// <summary>Gets a device by fingerprint.</summary>
    /// <param name="fingerprint">The device fingerprint.</param>
    /// <returns>The device when found; otherwise, null.</returns>
    Task<Device?> GetByFingerprintAsync(string fingerprint);

    /// <summary>Revokes all devices for a user.</summary>
    /// <param name="userId">The user identifier.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task RevokeAllForUserAsync(Guid userId);
}
