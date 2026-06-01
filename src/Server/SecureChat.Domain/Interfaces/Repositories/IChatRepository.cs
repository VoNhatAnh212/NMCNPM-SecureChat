#nullable enable

using SecureChat.Domain.Entities;

namespace SecureChat.Domain.Interfaces.Repositories;

/// <summary>
/// Defines chat persistence operations.
/// </summary>
public interface IChatRepository : IRepository<Chat>
{
    /// <summary>Gets a personal chat between two users.</summary>
    /// <param name="userId">The first user identifier.</param>
    /// <param name="otherUserId">The second user identifier.</param>
    /// <returns>The personal chat when found; otherwise, null.</returns>
    Task<Chat?> GetPersonalChatAsync(Guid userId, Guid otherUserId);

    /// <summary>Gets the chat that belongs to a group.</summary>
    /// <param name="groupId">The group identifier.</param>
    /// <returns>The group chat when found; otherwise, null.</returns>
    Task<Chat?> GetGroupChatAsync(Guid groupId);

    /// <summary>Gets all chats that belong to a group.</summary>
    /// <param name="groupId">The group identifier.</param>
    /// <returns>The group chats.</returns>
    Task<IReadOnlyCollection<Chat>> GetGroupChatsAsync(Guid groupId);

    /// <summary>Gets chats that include a user.</summary>
    /// <param name="userId">The user identifier.</param>
    /// <returns>The user's chats.</returns>
    Task<IReadOnlyCollection<Chat>> GetUserChatsAsync(Guid userId);
}
