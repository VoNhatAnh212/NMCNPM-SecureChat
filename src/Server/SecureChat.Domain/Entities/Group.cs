#nullable enable

namespace SecureChat.Domain.Entities;

/// <summary>
/// Represents a group chat aggregate.
/// </summary>
public class Group
{
    /// <summary>Initializes a new instance of the <see cref="Group"/> class.</summary>
    public Group()
    {
        Members = new HashSet<GroupMember>();
        Keys = new HashSet<GroupKey>();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Group"/> class with required fields.
    /// </summary>
    /// <param name="name">The group name.</param>
    /// <param name="creatorId">The creator user identifier.</param>
    public Group(string name, Guid creatorId)
        : this()
    {
        Id = Guid.NewGuid();
        Name = name;
        CreatorId = creatorId;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = CreatedAt;
        IsActive = true;
    }

    /// <summary>Gets or sets the group identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the group name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the group avatar URL.</summary>
    public string AvatarUrl { get; set; } = string.Empty;

    /// <summary>Gets or sets the creator user identifier.</summary>
    public Guid CreatorId { get; set; }

    /// <summary>Gets or sets the time when the group was created.</summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>Gets or sets the time when the group was last updated.</summary>
    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>Gets or sets a value indicating whether the group is active.</summary>
    public bool IsActive { get; set; }

    /// <summary>Gets or sets the group members.</summary>
    public ICollection<GroupMember> Members { get; set; }

    /// <summary>Gets or sets the encrypted group keys.</summary>
    public ICollection<GroupKey> Keys { get; set; }

    /// <summary>Gets or sets the creator user.</summary>
    public User Creator { get; set; } = null!;
}
