#nullable enable

using SecureChat.Domain.Entities;

namespace SecureChat.Domain.Interfaces.Repositories;

/// <summary>
/// Defines friend request persistence operations.
/// </summary>
public interface IFriendRequestRepository : IRepository<FriendRequest>
{
    /// <summary>Gets pending friend requests for a user.</summary>
    /// <param name="userId">The user identifier.</param>
    /// <returns>The pending friend requests.</returns>
    Task<IReadOnlyCollection<FriendRequest>> GetPendingForUserAsync(Guid userId);

    /// <summary>Gets pending friend requests sent by a user.</summary>
    /// <param name="userId">The user identifier.</param>
    /// <returns>The sent pending friend requests.</returns>
    Task<IReadOnlyCollection<FriendRequest>> GetPendingSentByUserAsync(Guid userId);

    /// <summary>Gets a friend request between two users.</summary>
    /// <param name="fromUserId">The sender user identifier.</param>
    /// <param name="toUserId">The recipient user identifier.</param>
    /// <returns>The friend request when found; otherwise, null.</returns>
    Task<FriendRequest?> GetBetweenUsersAsync(Guid fromUserId, Guid toUserId);
}
