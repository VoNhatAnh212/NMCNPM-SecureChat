#nullable enable

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SecureChat.Domain.Entities;
using SecureChat.Domain.Interfaces.Repositories;
using SecureChat.Infrastructure.Data;

namespace SecureChat.Infrastructure.Repositories;

/// <summary>
/// Provides Entity Framework Core persistence for group keys.
/// </summary>
public class GroupKeyRepository : BaseRepository<GroupKey>, IGroupKeyRepository
{
    private readonly ILogger<GroupKeyRepository> _logger;

    /// <summary>Initializes a new instance of the <see cref="GroupKeyRepository"/> class.</summary>
    public GroupKeyRepository(AppDbContext context, ILogger<GroupKeyRepository> logger, ILogger<BaseRepository<GroupKey>> baseLogger)
        : base(context, baseLogger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<GroupKey?> GetForDeviceAsync(Guid groupId, Guid deviceId)
    {
        _logger.LogDebug("Getting latest group key for group {GroupId} and device {DeviceId}.", groupId, deviceId);

        return await Context.GroupKeys
            .Where(groupKey => groupKey.GroupId == groupId && groupKey.ForDeviceId == deviceId)
            .OrderByDescending(groupKey => groupKey.KeyVersion)
            .ThenByDescending(groupKey => groupKey.CreatedAt)
            .FirstOrDefaultAsync();
    }

    /// <inheritdoc />
    public async Task<int> GetLatestVersionAsync(Guid groupId)
    {
        _logger.LogDebug("Getting latest group key version for group {GroupId}.", groupId);

        return await Context.GroupKeys
            .Where(groupKey => groupKey.GroupId == groupId)
            .Select(groupKey => (int?)groupKey.KeyVersion)
            .MaxAsync() ?? 0;
    }
}
