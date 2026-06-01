#nullable enable

using SecureChat.Shared.DTOs.Chat;
using SecureChat.Shared.DTOs.Message;

namespace SecureChat.Client.Services.Api;

/// <summary>
/// Provides chat API operations.
/// </summary>
public class ChatApiService
{
    private readonly IApiClient _apiClient;

    /// <summary>Initializes a new instance of the <see cref="ChatApiService"/> class.</summary>
    public ChatApiService(IApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    /// <summary>Gets the underlying API client.</summary>
    public IApiClient ApiClient => _apiClient;

    /// <summary>Gets chat summaries.</summary>
    public async Task<List<ChatSummaryDto>> GetChatsAsync() => (await _apiClient.GetAsync<IReadOnlyCollection<ChatSummaryDto>>("/api/chats")).ToList();

    /// <summary>Gets chat details.</summary>
    public Task<ChatDto> GetChatAsync(Guid chatId) => _apiClient.GetAsync<ChatDto>($"/api/chats/{chatId}");

    /// <summary>Gets paged chat messages.</summary>
    public async Task<List<MessageDto>> GetMessagesAsync(Guid chatId, int page) => (await _apiClient.GetAsync<IReadOnlyCollection<MessageDto>>($"/api/chats/{chatId}/messages?page={page}&size=50")).ToList();

    /// <summary>Gets or creates a personal chat.</summary>
    public Task<ChatDto> GetOrCreatePersonalChatAsync(Guid otherUserId) => _apiClient.PostAsync<object, ChatDto>($"/api/chats/personal/{otherUserId}", new { });

    /// <summary>Sends an encrypted message through HTTP.</summary>
    public Task<MessageDto> SendMessageAsync(SendMessageDto dto) => _apiClient.PostAsync<SendMessageDto, MessageDto>($"/api/chats/{dto.ChatId}/messages", dto);

    /// <summary>Marks messages in a chat as read.</summary>
    public Task MarkReadAsync(Guid chatId) => _apiClient.PostAsync<object, bool>($"/api/chats/{chatId}/read", new { });

    /// <summary>Exchanges an encrypted session key.</summary>
    public async Task ExchangeSessionKeyAsync(Guid chatId, SessionKeyDto sessionKeyDto) => await _apiClient.PostAsync<SessionKeyDto, bool>($"/api/chats/{chatId}/session-key", sessionKeyDto);

    /// <summary>Gets an encrypted session key.</summary>
    public Task<SessionKeyDto?> GetSessionKeyAsync(Guid chatId, string deviceId) => _apiClient.GetAsync<SessionKeyDto?>($"/api/chats/{chatId}/session-key/{deviceId}");
}
