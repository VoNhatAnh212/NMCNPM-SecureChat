#nullable enable

namespace SecureChat.Shared.Enums;

/// <summary>
/// Defines the lifecycle state of a message.
/// </summary>
public enum MessageStatus
{
    /// <summary>The message is being sent.</summary>
    Sending,
    /// <summary>The server accepted the message.</summary>
    Sent,
    /// <summary>The message reached a recipient device.</summary>
    Delivered,
    /// <summary>The recipient read the message.</summary>
    Read,
    /// <summary>The message failed to send or deliver.</summary>
    Failed
}
