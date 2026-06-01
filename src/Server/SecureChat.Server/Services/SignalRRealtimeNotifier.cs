#nullable enable

using Microsoft.AspNetCore.SignalR;
using SecureChat.Application.Services.Realtime;
using SecureChat.Server.Hubs;
using SecureChat.Shared.Constants;
using SecureChat.Shared.DTOs.Friend;
using SecureChat.Shared.DTOs.Message;

namespace SecureChat.Server.Services;

/// <summary>
/// Sends application realtime notifications through SignalR.
/// </summary>
public class SignalRRealtimeNotifier : IRealtimeNotifier
{
    private readonly IHubContext<ChatHub> _hubContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="SignalRRealtimeNotifier"/> class.
    /// </summary>
    public SignalRRealtimeNotifier(IHubContext<ChatHub> hubContext)
    {
        _hubContext = hubContext;
    }

    /// <inheritdoc />
    public async Task SendMessageToUserAsync(Guid userId, MessageDto message)
    {
        await _hubContext.Clients.Group(GetUserGroup(userId)).SendAsync(SignalREvents.ReceiveMessage, message);
    }

    /// <inheritdoc />
    public async Task SendFriendRequestAsync(Guid userId, FriendRequestDto request)
    {
        await _hubContext.Clients.Group(GetUserGroup(userId)).SendAsync(SignalREvents.FriendRequest, request);
    }

    /// <inheritdoc />
    public async Task SendFriendRequestResponseAsync(Guid userId, Guid requestId, bool accepted, Guid responderUserId)
    {
        await _hubContext.Clients.Group(GetUserGroup(userId)).SendAsync(SignalREvents.FriendRequestResponse, new
        {
            requestId,
            accepted,
            userId = responderUserId
        });
    }

    /// <inheritdoc />
    public async Task SendGroupUpdateAsync(Guid userId, Guid groupId, string eventType, object data)
    {
        await _hubContext.Clients.Group(GetUserGroup(userId)).SendAsync(SignalREvents.GroupUpdate, new
        {
            groupId,
            eventType,
            data
        });
    }

    /// <inheritdoc />
    public async Task SendDeviceUpdateAsync(Guid userId, string eventName, Guid deviceId)
    {
        await _hubContext.Clients.Group(GetUserGroup(userId)).SendAsync(SignalREvents.DeviceUpdate, new
        {
            @event = eventName,
            deviceId
        });
    }

    private static string GetUserGroup(Guid userId)
    {
        return $"user:{userId}";
    }
}
