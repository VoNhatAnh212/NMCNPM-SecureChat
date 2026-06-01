#nullable enable

using System.Text;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using SecureChat.Domain.Exceptions;
using SecureChat.Domain.Interfaces.Repositories;
using SecureChat.Shared.DTOs.Chat;
using SecureChat.Shared.DTOs.File;
using SecureChat.Shared.DTOs.Message;
using SecureChat.Shared.DTOs.User;
using SecureChat.Shared.Enums;
using DomainBlockedUser = SecureChat.Domain.Entities.BlockedUser;
using DomainChat = SecureChat.Domain.Entities.Chat;
using DomainMessage = SecureChat.Domain.Entities.Message;

namespace SecureChat.Application.Services.Message;

/// <summary>
/// Provides encrypted messaging workflows.
/// </summary>
public class MessageService : IMessageService
{
    private static readonly ConcurrentDictionary<string, SessionKeyDto> SessionKeys = new();

    private readonly IChatRepository _chatRepository;
    private readonly IMessageRepository _messageRepository;
    private readonly IAttachmentRepository _attachmentRepository;
    private readonly IUserRepository _userRepository;
    private readonly IFriendRepository _friendRepository;
    private readonly IRepository<DomainBlockedUser> _blockedUserRepository;
    private readonly ILogger<MessageService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="MessageService"/> class.
    /// </summary>
    public MessageService(
        IChatRepository chatRepository,
        IMessageRepository messageRepository,
        IAttachmentRepository attachmentRepository,
        IUserRepository userRepository,
        IFriendRepository friendRepository,
        IRepository<DomainBlockedUser> blockedUserRepository,
        ILogger<MessageService> logger)
    {
        _chatRepository = chatRepository;
        _messageRepository = messageRepository;
        _attachmentRepository = attachmentRepository;
        _userRepository = userRepository;
        _friendRepository = friendRepository;
        _blockedUserRepository = blockedUserRepository;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<MessageDto> SendMessageAsync(Guid senderId, SendMessageDto dto)
    {
        _logger.LogInformation("Sending encrypted message to chat {ChatId} from user {SenderId}.", dto.ChatId, senderId);

        var chat = await GetChatForMemberAsync(dto.ChatId, senderId);
        await EnsurePersonalChatNotBlockedAsync(chat, senderId);
        var sender = await _userRepository.GetByIdAsync(senderId) ?? throw new UserNotFoundException(senderId);

        var message = new DomainMessage(
            dto.ChatId,
            senderId,
            Decode(dto.EncryptedContent),
            dto.MessageType,
            Decode(dto.IV),
            Decode(dto.AuthTag))
        {
            Status = MessageStatus.Sent
        };

        await _messageRepository.AddAsync(message);
        foreach (var attachmentId in dto.AttachmentIds)
        {
            var attachment = await _attachmentRepository.GetByIdAsync(attachmentId);
            if (attachment is null)
            {
                continue;
            }

            attachment.MessageId = message.Id;
            await _attachmentRepository.UpdateAsync(attachment);
            message.Attachments.Add(attachment);
        }

        message.Chat = chat;
        message.Sender = sender;

        return MapMessage(message);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<MessageDto>> GetChatMessagesAsync(Guid chatId, Guid userId, int page, int size)
    {
        var chat = await GetChatForMemberAsync(chatId, userId);
        await EnsurePersonalChatNotBlockedAsync(chat, userId);
        var messages = await _messageRepository.GetByChatIdAsync(chatId, page, size);
        return messages.Select(MapMessage).ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<ChatSummaryDto>> GetChatListAsync(Guid userId)
    {
        var chats = await _chatRepository.GetUserChatsAsync(userId);
        return chats
            .Select(chat =>
            {
                var lastMessage = chat.Messages.OrderByDescending(message => message.SentAt).FirstOrDefault();
                return new ChatSummaryDto
                {
                    Id = chat.Id,
                    Name = chat.Type == ChatType.Group ? chat.Group?.Name ?? string.Empty : GetPersonalChatName(chat, userId),
                    AvatarUrl = chat.Type == ChatType.Group ? chat.Group?.AvatarUrl ?? string.Empty : string.Empty,
                    LastMessagePreview = lastMessage is null ? string.Empty : Encode(lastMessage.EncryptedContent),
                    LastMessageAt = lastMessage?.SentAt,
                    UnreadCount = chat.Messages.Count(message => message.SenderId != userId && message.ReadAt is null),
                    IsGroup = chat.Type == ChatType.Group
                };
            })
            .OrderByDescending(chat => chat.LastMessageAt)
            .ToList();
    }

    /// <inheritdoc />
    public async Task<ChatDto> GetOrCreatePersonalChatAsync(Guid userId, Guid otherUserId)
    {
        await EnsureUsersNotBlockedAsync(userId, otherUserId);
        if (!await _friendRepository.AreFriendsAsync(userId, otherUserId))
        {
            throw new UnauthorizedException("Users must be friends before starting a direct chat.");
        }

        var existingChat = await _chatRepository.GetPersonalChatAsync(userId, otherUserId);
        if (existingChat is not null)
        {
            return MapChat(existingChat);
        }

        var user = await _userRepository.GetByIdAsync(userId) ?? throw new UserNotFoundException(userId);
        var otherUser = await _userRepository.GetByIdAsync(otherUserId) ?? throw new UserNotFoundException(otherUserId);

        var chat = new DomainChat(ChatType.Personal);
        chat.Participants.Add(user);
        chat.Participants.Add(otherUser);

        await _chatRepository.AddAsync(chat);
        return MapChat(chat);
    }

    /// <inheritdoc />
    public async Task<ChatDto> GetChatAsync(Guid chatId, Guid userId)
    {
        var chat = await GetChatForMemberAsync(chatId, userId);
        await EnsurePersonalChatNotBlockedAsync(chat, userId);
        return MapChat(chat);
    }

    /// <inheritdoc />
    public async Task ExchangeSessionKeyAsync(Guid chatId, Guid senderId, SessionKeyDto sessionKeyDto)
    {
        _logger.LogInformation("Received encrypted session key exchange for chat {ChatId} and device {DeviceId}.", chatId, sessionKeyDto.DeviceId);
        var chat = await GetChatForMemberAsync(chatId, senderId);
        await EnsurePersonalChatNotBlockedAsync(chat, senderId);
        if (!chat.Participants.SelectMany(user => user.Devices).Any(device => device.Id == sessionKeyDto.DeviceId))
        {
            throw new UnauthorizedException("The target device is not a member of this chat.");
        }

        SessionKeys[GetSessionKeyKey(chatId, sessionKeyDto.DeviceId)] = sessionKeyDto with
        {
            ChatId = chatId,
            CreatedAt = sessionKeyDto.CreatedAt == default ? DateTimeOffset.UtcNow : sessionKeyDto.CreatedAt
        };
    }

    /// <inheritdoc />
    public async Task<SessionKeyDto?> GetSessionKeyAsync(Guid chatId, Guid deviceId, Guid requesterId)
    {
        var chat = await GetChatForMemberAsync(chatId, requesterId);
        await EnsurePersonalChatNotBlockedAsync(chat, requesterId);
        SessionKeys.TryGetValue(GetSessionKeyKey(chatId, deviceId), out var sessionKey);
        return sessionKey;
    }

    /// <inheritdoc />
    public async Task MarkMessagesReadAsync(Guid chatId, Guid userId)
    {
        await GetChatForMemberAsync(chatId, userId);
        await _messageRepository.MarkAsReadAsync(chatId, userId);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<Guid>> GetChatParticipantIdsAsync(Guid chatId)
    {
        var chat = await _chatRepository.GetByIdAsync(chatId) ?? throw new DomainException($"Chat '{chatId}' was not found.", 404);
        return chat.Participants.Select(user => user.Id).ToList();
    }

    private async Task<DomainChat> GetChatForMemberAsync(Guid chatId, Guid userId)
    {
        var chat = await _chatRepository.GetByIdAsync(chatId) ?? throw new DomainException($"Chat '{chatId}' was not found.", 404);
        if (!chat.Participants.Any(user => user.Id == userId))
        {
            throw new UnauthorizedException("The user is not a member of this chat.");
        }

        return chat;
    }

    private async Task EnsurePersonalChatNotBlockedAsync(DomainChat chat, Guid userId)
    {
        if (chat.Type != ChatType.Personal)
        {
            return;
        }

        var otherUserId = chat.Participants.FirstOrDefault(user => user.Id != userId)?.Id;
        if (otherUserId is null)
        {
            return;
        }

        await EnsureUsersNotBlockedAsync(userId, otherUserId.Value);
    }

    private async Task EnsureUsersNotBlockedAsync(Guid userId, Guid otherUserId)
    {
        var blocks = await _blockedUserRepository.GetAllAsync();
        if (blocks.Any(block =>
                (block.UserId == userId && block.BlockedUserId == otherUserId)
                || (block.UserId == otherUserId && block.BlockedUserId == userId)))
        {
            throw new DomainException("This chat is blocked. Messages and session key exchange are disabled.", 403);
        }
    }

    private static MessageDto MapMessage(DomainMessage message)
    {
        return new MessageDto
        {
            Id = message.Id,
            ChatId = message.ChatId,
            SenderId = message.SenderId,
            SenderName = message.Sender?.DisplayName ?? string.Empty,
            EncryptedContent = Encode(message.EncryptedContent),
            IV = Encode(message.IV),
            AuthTag = Encode(message.AuthTag),
            MessageType = message.MessageType,
            Status = message.Status,
            SentAt = message.SentAt,
            Attachments = message.Attachments.Select(attachment => new AttachmentDto
            {
                Id = attachment.Id,
                FileName = attachment.FileName,
                FileSize = attachment.FileSize,
                MimeType = attachment.MimeType,
                EncryptedUrl = attachment.EncryptedStoragePath,
                EncryptedFileKey = Encode(attachment.EncryptedFileKey),
                IV = Encode(attachment.IV),
                AuthTag = Encode(attachment.AuthTag)
            }).ToList()
        };
    }

    private static ChatDto MapChat(DomainChat chat)
    {
        var lastMessage = chat.Messages.OrderByDescending(message => message.SentAt).FirstOrDefault();
        return new ChatDto
        {
            Id = chat.Id,
            Type = chat.Type,
            Name = chat.Type == ChatType.Group ? chat.Group?.Name ?? string.Empty : string.Empty,
            AvatarUrl = chat.Type == ChatType.Group ? chat.Group?.AvatarUrl ?? string.Empty : string.Empty,
            Members = chat.Participants.Select(user => new UserDto
            {
                Id = user.Id,
                DisplayName = user.DisplayName,
                Email = user.Email,
                AvatarUrl = user.AvatarUrl,
                IsOnline = user.IsOnline,
                LastSeen = user.LastSeen
            }).ToList(),
            LastMessage = lastMessage is null ? null : MapMessage(lastMessage),
            UnreadCount = 0
        };
    }

    private static string GetPersonalChatName(DomainChat chat, Guid currentUserId)
    {
        return chat.Participants.FirstOrDefault(user => user.Id != currentUserId)?.DisplayName ?? string.Empty;
    }

    private static byte[] Decode(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Array.Empty<byte>();
        }

        try
        {
            return Convert.FromBase64String(value);
        }
        catch (FormatException)
        {
            return Encoding.UTF8.GetBytes(value);
        }
    }

    private static string Encode(byte[] value)
    {
        return Convert.ToBase64String(value);
    }

    private static string GetSessionKeyKey(Guid chatId, Guid deviceId)
    {
        return $"{chatId:N}:{deviceId:N}";
    }
}
