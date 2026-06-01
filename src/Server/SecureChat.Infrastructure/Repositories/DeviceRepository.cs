#nullable enable

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SecureChat.Domain.Entities;
using SecureChat.Domain.Exceptions;
using SecureChat.Domain.Interfaces.Repositories;
using SecureChat.Infrastructure.Data;
using SecureChat.Shared.Enums;

namespace SecureChat.Infrastructure.Repositories;

/// <summary>
/// Provides Entity Framework Core persistence for devices.
/// </summary>
public class DeviceRepository : BaseRepository<Device>, IDeviceRepository
{
    private readonly ILogger<DeviceRepository> _logger;

    /// <summary>Initializes a new instance of the <see cref="DeviceRepository"/> class.</summary>
    public DeviceRepository(AppDbContext context, ILogger<DeviceRepository> logger, ILogger<BaseRepository<Device>> baseLogger)
        : base(context, baseLogger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<Device>> GetByUserIdAsync(Guid userId)
    {
        _logger.LogDebug("Getting devices for user {UserId}.", userId);

        return await Context.Devices
            .Where(device => device.UserId == userId)
            .Include(device => device.PublicKey)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<Device?> GetByFingerprintAsync(string fingerprint)
    {
        _logger.LogDebug("Getting device by fingerprint.");

        return await Context.Devices
            .Include(device => device.PublicKey)
            .FirstOrDefaultAsync(device => device.DeviceFingerprint == fingerprint);
    }

    /// <inheritdoc />
    public async Task RevokeAllForUserAsync(Guid userId)
    {
        _logger.LogInformation("Revoking all devices for user {UserId}.", userId);

        var affectedRows = await Context.Devices
            .Where(device => device.UserId == userId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(device => device.Status, DeviceStatus.Revoked));

        if (affectedRows == 0)
        {
            throw new UserNotFoundException(userId);
        }
    }
}
