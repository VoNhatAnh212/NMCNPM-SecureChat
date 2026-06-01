#nullable enable

namespace SecureChat.Shared.Enums;

/// <summary>
/// Defines the relationship state between users.
/// </summary>
public enum FriendStatus
{
    /// <summary>No relationship exists.</summary>
    None,
    /// <summary>A friend request is pending.</summary>
    Pending,
    /// <summary>The users are friends.</summary>
    Friends,
    /// <summary>The relationship is blocked.</summary>
    Blocked
}
