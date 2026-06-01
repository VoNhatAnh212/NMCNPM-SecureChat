#nullable enable

using Microsoft.Extensions.Logging;
using SecureChat.Domain.Exceptions;
using SecureChat.Domain.Interfaces.Repositories;
using SecureChat.Shared.DTOs.Device;
using SecureChat.Shared.Enums;

namespace SecureChat.Application.Services.Device;

/// <summary>
/// Provides device management workflows.
/// </summary>
public class DeviceService : IDeviceService
{
    private readonly IDeviceRepository _deviceRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly ILogger<DeviceService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeviceService"/> class.
    /// </summary>
    public DeviceService(IDeviceRepository deviceRepository, IRefreshTokenRepository refreshTokenRepository, ILogger<DeviceService> logger)
    {
        _deviceRepository = deviceRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<DeviceDto>> GetUserDevicesAsync(Guid userId, Guid? currentDeviceId)
    {
        _logger.LogInformation("Getting devices for user {UserId}.", userId);

        var devices = await _deviceRepository.GetByUserIdAsync(userId);
        return devices
            .Select(device => new DeviceDto
            {
                Id = device.Id,
                DeviceName = device.DeviceName,
                Platform = device.Platform,
                LastLoginAt = device.LastLoginAt,
                IsCurrentDevice = currentDeviceId.HasValue && device.Id == currentDeviceId.Value,
                Fingerprint = device.DeviceFingerprint
            })
            .ToList();
    }

    /// <inheritdoc />
    public async Task RevokeDeviceAsync(Guid userId, Guid deviceId)
    {
        _logger.LogInformation("Revoking device {DeviceId} for user {UserId}.", deviceId, userId);

        var device = await _deviceRepository.GetByIdAsync(deviceId) ?? throw new DeviceNotFoundException(deviceId);
        if (device.UserId != userId)
        {
            throw new UnauthorizedException("The device does not belong to the current user.");
        }

        device.Status = DeviceStatus.Revoked;
        await _deviceRepository.UpdateAsync(device);
        await _refreshTokenRepository.RevokeAllForDeviceAsync(deviceId);
    }
}
