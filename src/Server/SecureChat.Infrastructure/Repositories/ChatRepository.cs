#nullable enable

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SecureChat.Domain.Entities;
using SecureChat.Domain.Interfaces.Repositories;
using SecureChat.Infrastructure.Data;
using SecureChat.Shared.Enums;

namespace SecureChat.Infrastructure.Repositories;

/// <summary>
/// Provides Entity Framework Core persistence for chats.
/// </summary>
public class ChatRepository : BaseRepository<Chat>, IChatRepository
{
    private readonly ILogger<ChatRepository> _logger;

    /// <summary>Initializes a new instance of the <see cref="ChatRepository"/> class.</summary>
    public ChatRepository(AppDbContext context, ILogger<ChatRepository> logger, ILogger<BaseRepository<Chat>> baseLogger)
        : base(context, baseLogger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public override async Task<Chat?> GetByIdAsync(Guid id)
    {
        _logger.LogDebug("Getting chat {ChatId} with participants.", id);

        return await Context.Chats
            .Include(chat => chat.Group)
            .Include(chat => chat.Participants)
            .ThenInclude(user => user.Devices)
            .Include(chat => chat.Messages)
            .ThenInclude(message => message.Attachments)
            .FirstOrDefaultAsync(chat => chat.Id == id);
    }

    /// <inheritdoc />
    public async Task<Chat?> GetPersonalChatAsync(Guid userId, Guid otherUserId)
    {
        _logger.LogDebug("Getting personal chat between {UserId} and {OtherUserId}.", userId, otherUserId);

        return await Context.Chats
            .Where(chat => chat.Type == ChatType.Personal)
            .Where(chat => chat.Participants.Any(user => user.Id == userId))
            .Where(chat => chat.Participants.Any(user => user.Id == otherUserId))
            .Include(chat => chat.Participants)
            .Include(chat => chat.Messages.OrderByDescending(message => message.SentAt).Take(1))
            .FirstOrDefaultAsync();
    }

    /// <inheritdoc />
    public async Task<Chat?> GetGroupChatAsync(Guid groupId)
    {
        _logger.LogDebug("Getting chat for group {GroupId}.", groupId);

        return (await GetGroupChatsAsync(groupId))
            .OrderByDescending(chat => chat.Participants.Count)
            .ThenByDescending(chat => chat.Messages.OrderByDescending(message => message.SentAt).FirstOrDefault()?.SentAt)
            .FirstOrDefault();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<Chat>> GetGroupChatsAsync(Guid groupId)
    {
        _logger.LogDebug("Getting all chats for group {GroupId}.", groupId);

        return await Context.Chats
            .Where(chat => chat.Type == ChatType.Group && chat.GroupId == groupId)
            .Include(chat => chat.Group)
            .Include(chat => chat.Participants)
            .ThenInclude(user => user.Devices)
            .Include(chat => chat.Messages.OrderByDescending(message => message.SentAt).Take(1))
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<Chat>> GetUserChatsAsync(Guid userId)
    {
        _logger.LogDebug("Getting chats for user {UserId}.", userId);

        return await Context.Chats
            .Where(chat => chat.Participants.Any(user => user.Id == userId))
            .Include(chat => chat.Group)
            .Include(chat => chat.Participants)
            .Include(chat => chat.Messages.OrderByDescending(message => message.SentAt).Take(1))
            .OrderByDescending(chat => chat.Messages.Max(message => (DateTimeOffset?)message.SentAt))
            .ToListAsync();
    }
}
