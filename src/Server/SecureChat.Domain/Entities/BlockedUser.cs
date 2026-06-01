#nullable enable

namespace SecureChat.Domain.Entities;

/// <summary>
/// Represents a blocked user relationship.
/// </summary>
public class BlockedUser
{
    /// <summary>Initializes a new instance of the <see cref="BlockedUser"/> class.</summary>
    public BlockedUser()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BlockedUser"/> class with required fields.
    /// </summary>
    /// <param name="userId">The blocking user identifier.</param>
    /// <param name="blockedUserId">The blocked user identifier.</param>
    public BlockedUser(Guid userId, Guid blockedUserId)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        BlockedUserId = blockedUserId;
        BlockedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Gets or sets the blocked user record identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the blocking user identifier.</summary>
    public Guid UserId { get; set; }

    /// <summary>Gets or sets the blocked user identifier.</summary>
    public Guid BlockedUserId { get; set; }

    /// <summary>Gets or sets the time when the user was blocked.</summary>
    public DateTimeOffset BlockedAt { get; set; }

    /// <summary>Gets or sets the blocking user.</summary>
    public User User { get; set; } = null!;

    /// <summary>Gets or sets the blocked user navigation property.</summary>
    public User BlockedUserNav { get; set; } = null!;
}
