#nullable enable

using SecureChat.Domain.Entities;

namespace SecureChat.Domain.Interfaces.Repositories;

/// <summary>
/// Defines friendship persistence operations.
/// </summary>
public interface IFriendRepository : IRepository<Friend>
{
    /// <summary>Gets friends for a user.</summary>
    /// <param name="userId">The user identifier.</param>
    /// <returns>The user's friendships.</returns>
    Task<IReadOnlyCollection<Friend>> GetFriendsAsync(Guid userId);

    /// <summary>Determines whether two users are friends.</summary>
    /// <param name="userId">The first user identifier.</param>
    /// <param name="friendUserId">The second user identifier.</param>
    /// <returns>True when the users are friends; otherwise, false.</returns>
    Task<bool> AreFriendsAsync(Guid userId, Guid friendUserId);

    /// <summary>Removes a friendship between two users.</summary>
    /// <param name="userId">The first user identifier.</param>
    /// <param name="friendUserId">The second user identifier.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task RemoveFriendAsync(Guid userId, Guid friendUserId);
}
