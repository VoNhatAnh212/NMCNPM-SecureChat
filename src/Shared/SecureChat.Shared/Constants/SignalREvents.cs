#nullable enable

namespace SecureChat.Shared.Constants;

/// <summary>
/// Contains SignalR event names used by SecureChat clients and hubs.
/// </summary>
public static class SignalREvents
{
    /// <summary>Event sent when a message is received.</summary>
    public const string ReceiveMessage = "ReceiveMessage";

    /// <summary>Event sent when a user typing state changes.</summary>
    public const string Typing = "Typing";

    /// <summary>Event sent when a user's online status changes.</summary>
    public const string OnlineStatus = "OnlineStatus";

    /// <summary>Event sent when a friend request changes.</summary>
    public const string FriendRequest = "FriendRequest";

    /// <summary>Event sent when a friend request is accepted or rejected.</summary>
    public const string FriendRequestResponse = "FriendRequestResponse";

    /// <summary>Event sent when group metadata or membership changes.</summary>
    public const string GroupUpdate = "GroupUpdate";

    /// <summary>Event sent when a device registration changes.</summary>
    public const string DeviceUpdate = "DeviceUpdate";

    /// <summary>Event sent when a message status changes.</summary>
    public const string MessageStatus = "MessageStatus";

    /// <summary>Event sent when encrypted file transfer progress changes.</summary>
    public const string FileTransferProgress = "FileTransferProgress";
}
