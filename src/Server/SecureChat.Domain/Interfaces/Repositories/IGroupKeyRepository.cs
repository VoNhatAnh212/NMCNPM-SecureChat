#nullable enable

using SecureChat.Domain.Entities;

namespace SecureChat.Domain.Interfaces.Repositories;

/// <summary>
/// Defines group key persistence operations.
/// </summary>
public interface IGroupKeyRepository : IRepository<GroupKey>
{
    /// <summary>Gets group keys encrypted for a device.</summary>
    /// <param name="groupId">The group identifier.</param>
    /// <param name="deviceId">The device identifier.</param>
    /// <returns>The latest group key for the device when found; otherwise, null.</returns>
    Task<GroupKey?> GetForDeviceAsync(Guid groupId, Guid deviceId);

    /// <summary>Gets the latest key version for a group.</summary>
    /// <param name="groupId">The group identifier.</param>
    /// <returns>The latest key version.</returns>
    Task<int> GetLatestVersionAsync(Guid groupId);
}
