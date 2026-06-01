#nullable enable

using SecureChat.Shared.Enums;

namespace SecureChat.Domain.Entities;

/// <summary>
/// Represents a friend request between users.
/// </summary>
public class FriendRequest
{
    /// <summary>Initializes a new instance of the <see cref="FriendRequest"/> class.</summary>
    public FriendRequest()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FriendRequest"/> class with required fields.
    /// </summary>
    /// <param name="fromUserId">The sender user identifier.</param>
    /// <param name="toUserId">The recipient user identifier.</param>
    /// <param name="message">The request message.</param>
    public FriendRequest(Guid fromUserId, Guid toUserId, string message)
    {
        Id = Guid.NewGuid();
        FromUserId = fromUserId;
        ToUserId = toUserId;
        Message = message;
        Status = FriendStatus.Pending;
        SentAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Gets or sets the friend request identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the sender user identifier.</summary>
    public Guid FromUserId { get; set; }

    /// <summary>Gets or sets the recipient user identifier.</summary>
    public Guid ToUserId { get; set; }

    /// <summary>Gets or sets the request message.</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>Gets or sets the friend request status.</summary>
    public FriendStatus Status { get; set; }

    /// <summary>Gets or sets the time when the request was sent.</summary>
    public DateTimeOffset SentAt { get; set; }

    /// <summary>Gets or sets the time when the request was answered.</summary>
    public DateTimeOffset? RespondedAt { get; set; }

    /// <summary>Gets or sets the sender user.</summary>
    public User FromUser { get; set; } = null!;

    /// <summary>Gets or sets the recipient user.</summary>
    public User ToUser { get; set; } = null!;
}
