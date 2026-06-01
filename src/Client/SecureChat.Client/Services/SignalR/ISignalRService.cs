#nullable enable

using SecureChat.Shared.DTOs.Friend;
using SecureChat.Shared.DTOs.Message;

namespace SecureChat.Client.Services.SignalR;

/// <summary>
/// Defines client SignalR operations.
/// </summary>
public interface ISignalRService
{
    /// <summary>Occurs when a message is received.</summary>
    event EventHandler<MessageDto>? MessageReceived;

    /// <summary>Occurs when typing status is received.</summary>
    event EventHandler<TypingEventArgs>? TypingReceived;

    /// <summary>Occurs when online status changes.</summary>
    event EventHandler<OnlineStatusEventArgs>? OnlineStatusChanged;

    /// <summary>Occurs when a friend request is received.</summary>
    event EventHandler<FriendRequestDto>? FriendRequestReceived;

    /// <summary>Occurs when a friend request response is received.</summary>
    event EventHandler<string>? FriendRequestResponded;

    /// <summary>Occurs when a group is updated.</summary>
    event EventHandler<GroupUpdateEventArgs>? GroupUpdated;

    /// <summary>Occurs when a device is revoked.</summary>
    event EventHandler<string>? DeviceRevoked;

    /// <summary>Gets a value indicating whether the hub is connected.</summary>
    bool IsConnected { get; }

    /// <summary>Connects to the SignalR hub.</summary>
    Task ConnectAsync(string accessToken);

    /// <summary>Disconnects from the SignalR hub.</summary>
    Task DisconnectAsync();

    /// <summary>Sends an encrypted message.</summary>
    Task SendMessageAsync(SendMessageDto dto);

    /// <summary>Sends typing status.</summary>
    Task SendTypingAsync(Guid chatId, bool isTyping);

}
