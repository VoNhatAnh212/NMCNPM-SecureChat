#nullable enable

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SecureChat.Domain.Entities;
using SecureChat.Domain.Interfaces.Repositories;
using SecureChat.Infrastructure.Data;

namespace SecureChat.Infrastructure.Repositories;

/// <summary>
/// Provides Entity Framework Core persistence for groups.
/// </summary>
public class GroupRepository : BaseRepository<Group>, IGroupRepository
{
    private readonly ILogger<GroupRepository> _logger;

    /// <summary>Initializes a new instance of the <see cref="GroupRepository"/> class.</summary>
    public GroupRepository(AppDbContext context, ILogger<GroupRepository> logger, ILogger<BaseRepository<Group>> baseLogger)
        : base(context, baseLogger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<Group>> GetByMemberIdAsync(Guid memberId)
    {
        _logger.LogDebug("Getting groups for member {MemberId}.", memberId);

        return await Context.Groups
            .Where(group => group.IsActive && group.Members.Any(member => member.UserId == memberId))
            .Include(group => group.Members)
            .ThenInclude(member => member.User)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<Group?> GetWithMembersAsync(Guid groupId)
    {
        _logger.LogDebug("Getting group {GroupId} with members.", groupId);

        return await Context.Groups
            .Include(group => group.Members)
            .ThenInclude(member => member.User)
            .FirstOrDefaultAsync(group => group.Id == groupId && group.IsActive);
    }
}
