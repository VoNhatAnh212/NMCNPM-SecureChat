#nullable enable

namespace SecureChat.Domain.Entities;

/// <summary>
/// Represents a friendship edge between two users.
/// </summary>
public class Friend
{
    /// <summary>Initializes a new instance of the <see cref="Friend"/> class.</summary>
    public Friend()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Friend"/> class with required fields.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="friendUserId">The friend's user identifier.</param>
    public Friend(Guid userId, Guid friendUserId)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        FriendUserId = friendUserId;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Gets or sets the friendship identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the user identifier.</summary>
    public Guid UserId { get; set; }

    /// <summary>Gets or sets the friend's user identifier.</summary>
    public Guid FriendUserId { get; set; }

    /// <summary>Gets or sets the time when the friendship was created.</summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>Gets or sets the user.</summary>
    public User User { get; set; } = null!;

    /// <summary>Gets or sets the friend user.</summary>
    public User FriendUser { get; set; } = null!;
}
