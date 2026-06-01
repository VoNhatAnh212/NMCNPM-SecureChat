#nullable enable

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SecureChat.Domain.Entities;
using SecureChat.Domain.Interfaces.Repositories;
using SecureChat.Infrastructure.Data;

namespace SecureChat.Infrastructure.Repositories;

/// <summary>
/// Provides Entity Framework Core persistence for public keys.
/// </summary>
public class PublicKeyRepository : BaseRepository<PublicKey>, IPublicKeyRepository
{
    private readonly ILogger<PublicKeyRepository> _logger;

    /// <summary>Initializes a new instance of the <see cref="PublicKeyRepository"/> class.</summary>
    public PublicKeyRepository(AppDbContext context, ILogger<PublicKeyRepository> logger, ILogger<BaseRepository<PublicKey>> baseLogger)
        : base(context, baseLogger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<PublicKey?> GetActiveByDeviceIdAsync(Guid deviceId)
    {
        _logger.LogDebug("Getting active public key for device {DeviceId}.", deviceId);

        return await Context.PublicKeys
            .Include(publicKey => publicKey.Device)
            .FirstOrDefaultAsync(publicKey => publicKey.DeviceId == deviceId && publicKey.IsActive);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<PublicKey>> GetByUserIdAsync(Guid userId)
    {
        _logger.LogDebug("Getting public keys for user {UserId}.", userId);

        return await Context.PublicKeys
            .Where(publicKey => publicKey.UserId == userId)
            .Include(publicKey => publicKey.Device)
            .ToListAsync();
    }
}
