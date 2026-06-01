#nullable enable

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SecureChat.Domain.Entities;
using SecureChat.Domain.Interfaces.Repositories;
using SecureChat.Infrastructure.Data;

namespace SecureChat.Infrastructure.Repositories;

/// <summary>
/// Provides Entity Framework Core persistence for friendships.
/// </summary>
public class FriendRepository : BaseRepository<Friend>, IFriendRepository
{
    private readonly ILogger<FriendRepository> _logger;

    /// <summary>Initializes a new instance of the <see cref="FriendRepository"/> class.</summary>
    public FriendRepository(AppDbContext context, ILogger<FriendRepository> logger, ILogger<BaseRepository<Friend>> baseLogger)
        : base(context, baseLogger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<Friend>> GetFriendsAsync(Guid userId)
    {
        _logger.LogDebug("Getting friends for user {UserId}.", userId);

        return await Context.Friends
            .Where(friend => friend.UserId == userId || friend.FriendUserId == userId)
            .Include(friend => friend.User)
            .Include(friend => friend.FriendUser)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<bool> AreFriendsAsync(Guid userId, Guid friendUserId)
    {
        _logger.LogDebug("Checking friendship between {UserId} and {FriendUserId}.", userId, friendUserId);

        return await Context.Friends.AnyAsync(friend =>
            (friend.UserId == userId && friend.FriendUserId == friendUserId)
            || (friend.UserId == friendUserId && friend.FriendUserId == userId));
    }

    /// <inheritdoc />
    public async Task RemoveFriendAsync(Guid userId, Guid friendUserId)
    {
        _logger.LogInformation("Removing friendship between {UserId} and {FriendUserId}.", userId, friendUserId);

        var friendships = await Context.Friends
            .Where(friend =>
                (friend.UserId == userId && friend.FriendUserId == friendUserId)
                || (friend.UserId == friendUserId && friend.FriendUserId == userId))
            .ToListAsync();

        if (friendships.Count == 0)
        {
            return;
        }

        Context.Friends.RemoveRange(friendships);
        await Context.SaveChangesAsync();
    }
}
