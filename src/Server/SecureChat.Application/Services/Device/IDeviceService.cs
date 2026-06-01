#nullable enable

using SecureChat.Shared.DTOs.Device;

namespace SecureChat.Application.Services.Device;

/// <summary>
/// Defines device management application operations.
/// </summary>
public interface IDeviceService
{
    /// <summary>Gets devices registered to a user.</summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="currentDeviceId">The current device identifier.</param>
    /// <returns>The user's devices.</returns>
    Task<IReadOnlyCollection<DeviceDto>> GetUserDevicesAsync(Guid userId, Guid? currentDeviceId);

    /// <summary>Revokes a device owned by a user.</summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="deviceId">The device identifier.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task RevokeDeviceAsync(Guid userId, Guid deviceId);
}
