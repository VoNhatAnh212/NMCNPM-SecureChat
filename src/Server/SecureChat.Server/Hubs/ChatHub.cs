#nullable enable

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using SecureChat.Application.Services.Friend;
using SecureChat.Application.Services.Message;
using SecureChat.Domain.Interfaces.Repositories;
using SecureChat.Server.Extensions;
using SecureChat.Shared.Constants;
using SecureChat.Shared.DTOs.Message;

namespace SecureChat.Server.Hubs;

/// <summary>
/// Provides realtime chat operations for authenticated clients.
/// </summary>
[Authorize]
public class ChatHub : Hub
{
    private static readonly ConcurrentDictionary<Guid, int> ConnectionCounts = new();

    private readonly IUserRepository _userRepository;
    private readonly IFriendService _friendService;
    private readonly IMessageService _messageService;
    private readonly ILogger<ChatHub> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChatHub"/> class.
    /// </summary>
    public ChatHub(IUserRepository userRepository, IFriendService friendService, IMessageService messageService, ILogger<ChatHub> logger)
    {
        _userRepository = userRepository;
        _friendService = friendService;
        _messageService = messageService;
        _logger = logger;
    }

    /// <inheritdoc />
    public override async Task OnConnectedAsync()
    {
        var userId = Context.User!.GetUserId();
        await Groups.AddToGroupAsync(Context.ConnectionId, GetUserGroup(userId));

        var connectionCount = ConnectionCounts.AddOrUpdate(userId, 1, (_, count) => count + 1);
        if (connectionCount == 1)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user is not null)
            {
                user.IsOnline = true;
                user.LastSeen = DateTimeOffset.UtcNow;
                await _userRepository.UpdateAsync(user);
            }

            await BroadcastOnlineStatusToFriendsAsync(userId, true, user?.LastSeen);
        }

        await base.OnConnectedAsync();
    }

    /// <inheritdoc />
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.User!.GetUserId();
        var lastSeen = DateTimeOffset.UtcNow;

        var remainingConnections = ConnectionCounts.AddOrUpdate(userId, 0, (_, count) => Math.Max(0, count - 1));
        if (remainingConnections == 0)
        {
            ConnectionCounts.TryRemove(userId, out _);

            var user = await _userRepository.GetByIdAsync(userId);
            if (user is not null)
            {
                user.IsOnline = false;
                user.LastSeen = lastSeen;
                await _userRepository.UpdateAsync(user);
            }

            await BroadcastOnlineStatusToFriendsAsync(userId, false, lastSeen);
        }

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Sends an encrypted message to a chat.
    /// </summary>
    public async Task SendMessageAsync(SendMessageDto dto)
    {
        var senderId = Context.User!.GetUserId();
        var message = await _messageService.SendMessageAsync(senderId, dto);
        var recipientIds = await _messageService.GetChatParticipantIdsAsync(dto.ChatId);

        foreach (var recipientId in recipientIds.Where(id => id != senderId))
        {
            await Clients.Group(GetUserGroup(recipientId)).SendAsync(SignalREvents.ReceiveMessage, message);
        }
    }

    /// <summary>
    /// Broadcasts typing state to chat participants.
    /// </summary>
    public async Task SendTypingAsync(Guid chatId, bool isTyping)
    {
        var userId = Context.User!.GetUserId();
        var user = await _userRepository.GetByIdAsync(userId);
        var participantIds = await _messageService.GetChatParticipantIdsAsync(chatId);

        foreach (var participantId in participantIds.Where(id => id != userId))
        {
            await Clients.Group(GetUserGroup(participantId)).SendAsync(SignalREvents.Typing, new
            {
                chatId,
                userId,
                displayName = user?.DisplayName ?? string.Empty,
                isTyping
            });
        }
    }

    private async Task BroadcastOnlineStatusToFriendsAsync(Guid userId, bool isOnline, DateTimeOffset? lastSeen)
    {
        var friends = await _friendService.GetFriendsAsync(userId);
        foreach (var friend in friends)
        {
            await Clients.Group(GetUserGroup(friend.UserId)).SendAsync(SignalREvents.OnlineStatus, new
            {
                userId,
                isOnline,
                lastSeen
            });
        }
    }

    private static string GetUserGroup(Guid userId)
    {
        return $"user:{userId}";
    }
}
