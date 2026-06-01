#nullable enable

using SecureChat.Domain.Entities;

namespace SecureChat.Domain.Interfaces.Repositories;

/// <summary>
/// Defines message persistence operations.
/// </summary>
public interface IMessageRepository : IRepository<Message>
{
    /// <summary>Gets messages for a chat.</summary>
    /// <param name="chatId">The chat identifier.</param>
    /// <param name="page">The one-based page number.</param>
    /// <param name="size">The page size.</param>
    /// <returns>The chat messages.</returns>
    Task<IReadOnlyCollection<Message>> GetByChatIdAsync(Guid chatId, int page, int size);

    /// <summary>Gets undelivered messages for a user.</summary>
    /// <param name="userId">The recipient user identifier.</param>
    /// <returns>The undelivered messages.</returns>
    Task<IReadOnlyCollection<Message>> GetUndeliveredAsync(Guid userId);

    /// <summary>Marks messages in a chat as read for a user.</summary>
    /// <param name="chatId">The chat identifier.</param>
    /// <param name="userId">The user identifier.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task MarkAsReadAsync(Guid chatId, Guid userId);
}
