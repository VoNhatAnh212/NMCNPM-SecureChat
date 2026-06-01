#nullable enable

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SecureChat.Domain.Entities;
using SecureChat.Domain.Interfaces.Repositories;
using SecureChat.Infrastructure.Data;

namespace SecureChat.Infrastructure.Repositories;

/// <summary>
/// Provides Entity Framework Core persistence for refresh tokens.
/// </summary>
public class RefreshTokenRepository : BaseRepository<RefreshToken>, IRefreshTokenRepository
{
    private readonly ILogger<RefreshTokenRepository> _logger;

    /// <summary>Initializes a new instance of the <see cref="RefreshTokenRepository"/> class.</summary>
    public RefreshTokenRepository(AppDbContext context, ILogger<RefreshTokenRepository> logger, ILogger<BaseRepository<RefreshToken>> baseLogger)
        : base(context, baseLogger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<RefreshToken?> GetActiveTokenAsync(string token)
    {
        _logger.LogDebug("Getting active refresh token.");

        var utcNow = DateTimeOffset.UtcNow;

        return await Context.RefreshTokens
            .Include(refreshToken => refreshToken.User)
            .Include(refreshToken => refreshToken.Device)
            .FirstOrDefaultAsync(refreshToken =>
                refreshToken.Token == token
                && !refreshToken.IsRevoked
                && refreshToken.ExpiresAt > utcNow);
    }

    /// <inheritdoc />
    public async Task RevokeAllForDeviceAsync(Guid deviceId)
    {
        _logger.LogInformation("Revoking all refresh tokens for device {DeviceId}.", deviceId);

        var utcNow = DateTimeOffset.UtcNow;

        await Context.RefreshTokens
            .Where(refreshToken => refreshToken.DeviceId == deviceId && !refreshToken.IsRevoked)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(refreshToken => refreshToken.IsRevoked, true)
                .SetProperty(refreshToken => refreshToken.RevokedAt, utcNow));
    }
}
