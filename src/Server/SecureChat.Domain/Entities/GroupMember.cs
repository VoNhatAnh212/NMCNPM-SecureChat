#nullable enable

using SecureChat.Shared.Enums;

namespace SecureChat.Domain.Entities;

/// <summary>
/// Represents membership of a user in a group.
/// </summary>
public class GroupMember
{
    /// <summary>Initializes a new instance of the <see cref="GroupMember"/> class.</summary>
    public GroupMember()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GroupMember"/> class with required fields.
    /// </summary>
    /// <param name="groupId">The group identifier.</param>
    /// <param name="userId">The user identifier.</param>
    /// <param name="role">The member role.</param>
    public GroupMember(Guid groupId, Guid userId, GroupRole role)
    {
        Id = Guid.NewGuid();
        GroupId = groupId;
        UserId = userId;
        Role = role;
        JoinedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Gets or sets the group member identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the group identifier.</summary>
    public Guid GroupId { get; set; }

    /// <summary>Gets or sets the user identifier.</summary>
    public Guid UserId { get; set; }

    /// <summary>Gets or sets the member role.</summary>
    public GroupRole Role { get; set; }

    /// <summary>Gets or sets the time when the user joined the group.</summary>
    public DateTimeOffset JoinedAt { get; set; }

    /// <summary>Gets or sets the group.</summary>
    public Group Group { get; set; } = null!;

    /// <summary>Gets or sets the user.</summary>
    public User User { get; set; } = null!;
}
