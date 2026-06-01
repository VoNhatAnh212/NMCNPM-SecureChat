#nullable enable

using SecureChat.Domain.Entities;

namespace SecureChat.Domain.Interfaces.Repositories;

/// <summary>
/// Defines user persistence operations.
/// </summary>
public interface IUserRepository : IRepository<User>
{
    /// <summary>Gets a user by email address.</summary>
    /// <param name="email">The email address.</param>
    /// <returns>The user when found; otherwise, null.</returns>
    Task<User?> GetByEmailAsync(string email);

    /// <summary>Gets currently online users.</summary>
    /// <returns>The online users.</returns>
    Task<IReadOnlyCollection<User>> GetOnlineUsersAsync();

    /// <summary>Searches users by text.</summary>
    /// <param name="query">The search text.</param>
    /// <param name="limit">The maximum number of users to return.</param>
    /// <returns>The matching users.</returns>
    Task<IReadOnlyCollection<User>> SearchAsync(string query, int limit);
}
