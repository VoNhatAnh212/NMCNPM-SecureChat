#nullable enable

using SecureChat.Shared.DTOs.Friend;
using SecureChat.Shared.DTOs.Message;

namespace SecureChat.Application.Services.Realtime;

/// <summary>
/// Defines realtime notification operations used by application services.
/// </summary>
public interface IRealtimeNotifier
{
    /// <summary>Sends a message to a user group.</summary>
    Task SendMessageToUserAsync(Guid userId, MessageDto message);

    /// <summary>Sends a friend request notification to a user group.</summary>
    Task SendFriendRequestAsync(Guid userId, FriendRequestDto request);

    /// <summary>Sends a friend request response notification to a user group.</summary>
    Task SendFriendRequestResponseAsync(Guid userId, Guid requestId, bool accepted, Guid responderUserId);

    /// <summary>Sends a group update notification to a user group.</summary>
    Task SendGroupUpdateAsync(Guid userId, Guid groupId, string eventType, object data);

    /// <summary>Sends a device update notification to a user group.</summary>
    Task SendDeviceUpdateAsync(Guid userId, string eventName, Guid deviceId);
}
