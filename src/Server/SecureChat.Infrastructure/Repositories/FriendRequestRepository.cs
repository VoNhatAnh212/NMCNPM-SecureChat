#nullable enable

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SecureChat.Domain.Entities;
using SecureChat.Domain.Interfaces.Repositories;
using SecureChat.Infrastructure.Data;
using SecureChat.Shared.Enums;

namespace SecureChat.Infrastructure.Repositories;

/// <summary>
/// Provides Entity Framework Core persistence for friend requests.
/// </summary>
public class FriendRequestRepository : BaseRepository<FriendRequest>, IFriendRequestRepository
{
    private readonly ILogger<FriendRequestRepository> _logger;

    /// <summary>Initializes a new instance of the <see cref="FriendRequestRepository"/> class.</summary>
    public FriendRequestRepository(AppDbContext context, ILogger<FriendRequestRepository> logger, ILogger<BaseRepository<FriendRequest>> baseLogger)
        : base(context, baseLogger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<FriendRequest>> GetPendingForUserAsync(Guid userId)
    {
        _logger.LogDebug("Getting pending friend requests for user {UserId}.", userId);

        return await Context.FriendRequests
            .Where(request => request.ToUserId == userId && request.Status == FriendStatus.Pending)
            .Include(request => request.FromUser)
            .Include(request => request.ToUser)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<FriendRequest>> GetPendingSentByUserAsync(Guid userId)
    {
        _logger.LogDebug("Getting sent pending friend requests for user {UserId}.", userId);

        return await Context.FriendRequests
            .Where(request => request.FromUserId == userId && request.Status == FriendStatus.Pending)
            .Include(request => request.FromUser)
            .Include(request => request.ToUser)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<FriendRequest?> GetBetweenUsersAsync(Guid fromUserId, Guid toUserId)
    {
        _logger.LogDebug("Getting friend request between {FromUserId} and {ToUserId}.", fromUserId, toUserId);

        return await Context.FriendRequests
            .Include(request => request.FromUser)
            .Include(request => request.ToUser)
            .FirstOrDefaultAsync(request =>
                (request.FromUserId == fromUserId && request.ToUserId == toUserId)
                || (request.FromUserId == toUserId && request.ToUserId == fromUserId));
    }
}
