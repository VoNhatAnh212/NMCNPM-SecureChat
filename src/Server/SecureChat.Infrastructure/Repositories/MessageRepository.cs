#nullable enable

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SecureChat.Domain.Entities;
using SecureChat.Domain.Interfaces.Repositories;
using SecureChat.Infrastructure.Data;
using SecureChat.Shared.Enums;

namespace SecureChat.Infrastructure.Repositories;

/// <summary>
/// Provides Entity Framework Core persistence for messages.
/// </summary>
public class MessageRepository : BaseRepository<Message>, IMessageRepository
{
    private readonly ILogger<MessageRepository> _logger;

    /// <summary>Initializes a new instance of the <see cref="MessageRepository"/> class.</summary>
    public MessageRepository(AppDbContext context, ILogger<MessageRepository> logger, ILogger<BaseRepository<Message>> baseLogger)
        : base(context, baseLogger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<Message>> GetByChatIdAsync(Guid chatId, int page, int size)
    {
        _logger.LogDebug("Getting messages for chat {ChatId}, page {Page}, size {Size}.", chatId, page, size);

        var pageNumber = Math.Max(1, page);
        var pageSize = Math.Max(1, size);

        return await Context.Messages
            .Where(message => message.ChatId == chatId && !message.IsDeleted)
            .Include(message => message.Sender)
            .Include(message => message.Attachments)
            .OrderByDescending(message => message.SentAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<Message>> GetUndeliveredAsync(Guid userId)
    {
        _logger.LogDebug("Getting undelivered messages for user {UserId}.", userId);

        return await Context.Messages
            .Where(message => message.Status != MessageStatus.Delivered && message.Status != MessageStatus.Read)
            .Where(message => message.SenderId != userId)
            .Where(message => message.Chat.Participants.Any(participant => participant.Id == userId))
            .Include(message => message.Chat)
            .Include(message => message.Sender)
            .Include(message => message.Attachments)
            .OrderBy(message => message.SentAt)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task MarkAsReadAsync(Guid chatId, Guid userId)
    {
        _logger.LogInformation("Marking messages as read for chat {ChatId} and user {UserId}.", chatId, userId);

        var utcNow = DateTimeOffset.UtcNow;

        await Context.Messages
            .Where(message => message.ChatId == chatId)
            .Where(message => message.SenderId != userId)
            .Where(message => message.ReadAt == null)
            .Where(message => message.Chat.Participants.Any(participant => participant.Id == userId))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(message => message.ReadAt, utcNow)
                .SetProperty(message => message.Status, MessageStatus.Read));
    }
}
