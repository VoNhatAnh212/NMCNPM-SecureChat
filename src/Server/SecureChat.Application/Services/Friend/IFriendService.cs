#nullable enable

using SecureChat.Shared.DTOs.Friend;

namespace SecureChat.Application.Services.Friend;

/// <summary>
/// Defines friend management application operations.
/// </summary>
public interface IFriendService
{
    /// <summary>Sends a friend request.</summary>
    Task<FriendRequestDto> SendFriendRequestAsync(Guid fromUserId, Guid toUserId, string message);

    /// <summary>Responds to a friend request.</summary>
    Task<bool> RespondToRequestAsync(Guid requestId, Guid userId, bool accept);

    /// <summary>Gets friends for a user.</summary>
    Task<IReadOnlyCollection<FriendDto>> GetFriendsAsync(Guid userId);

    /// <summary>Gets pending friend requests for a user.</summary>
    Task<IReadOnlyCollection<FriendRequestDto>> GetPendingRequestsAsync(Guid userId);

    /// <summary>Gets pending friend requests sent by a user.</summary>
    Task<IReadOnlyCollection<FriendRequestDto>> GetSentPendingRequestsAsync(Guid userId);

    /// <summary>Removes a friend relationship.</summary>
    Task RemoveFriendAsync(Guid userId, Guid friendId);

    /// <summary>Blocks a user.</summary>
    Task BlockUserAsync(Guid userId, Guid targetId);

    /// <summary>Unblocks a user.</summary>
    Task UnblockUserAsync(Guid userId, Guid targetId);

    /// <summary>Gets blocked users for a user.</summary>
    Task<IReadOnlyCollection<BlockedUserDto>> GetBlockedUsersAsync(Guid userId);
}
