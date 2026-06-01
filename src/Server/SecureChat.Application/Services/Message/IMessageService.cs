#nullable enable

using SecureChat.Shared.DTOs.Chat;
using SecureChat.Shared.DTOs.Message;

namespace SecureChat.Application.Services.Message;

/// <summary>
/// Defines encrypted messaging application operations.
/// </summary>
public interface IMessageService
{
    /// <summary>Sends and persists an encrypted message.</summary>
    Task<MessageDto> SendMessageAsync(Guid senderId, SendMessageDto dto);

    /// <summary>Gets encrypted messages for a chat.</summary>
    Task<IReadOnlyCollection<MessageDto>> GetChatMessagesAsync(Guid chatId, Guid userId, int page, int size);

    /// <summary>Gets chat summaries for a user.</summary>
    Task<IReadOnlyCollection<ChatSummaryDto>> GetChatListAsync(Guid userId);

    /// <summary>Gets or creates a personal chat between two users.</summary>
    Task<ChatDto> GetOrCreatePersonalChatAsync(Guid userId, Guid otherUserId);

    /// <summary>Gets a chat for a member.</summary>
    Task<ChatDto> GetChatAsync(Guid chatId, Guid userId);

    /// <summary>Stores an encrypted session key exchange payload.</summary>
    Task ExchangeSessionKeyAsync(Guid chatId, Guid senderId, SessionKeyDto sessionKeyDto);

    /// <summary>Gets an encrypted session key for a device.</summary>
    Task<SessionKeyDto?> GetSessionKeyAsync(Guid chatId, Guid deviceId, Guid requesterId);

    /// <summary>Marks messages as read for a user in a chat.</summary>
    Task MarkMessagesReadAsync(Guid chatId, Guid userId);

    /// <summary>Gets chat participant identifiers.</summary>
    Task<IReadOnlyCollection<Guid>> GetChatParticipantIdsAsync(Guid chatId);
}
