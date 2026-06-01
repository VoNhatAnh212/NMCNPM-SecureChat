#nullable enable

using SecureChat.Shared.Enums;

namespace SecureChat.Domain.Entities;

/// <summary>
/// Represents a chat conversation.
/// </summary>
public class Chat
{
    /// <summary>Initializes a new instance of the <see cref="Chat"/> class.</summary>
    public Chat()
    {
        Messages = new HashSet<Message>();
        Participants = new HashSet<User>();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Chat"/> class with required fields.
    /// </summary>
    /// <param name="type">The chat type.</param>
    /// <param name="groupId">The optional group identifier.</param>
    public Chat(ChatType type, Guid? groupId = null)
        : this()
    {
        Id = Guid.NewGuid();
        Type = type;
        GroupId = groupId;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Gets or sets the chat identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the chat type.</summary>
    public ChatType Type { get; set; }

    /// <summary>Gets or sets the associated group identifier, when this is a group chat.</summary>
    public Guid? GroupId { get; set; }

    /// <summary>Gets or sets the time when the chat was created.</summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>Gets or sets the associated group.</summary>
    public Group? Group { get; set; }

    /// <summary>Gets or sets the chat messages.</summary>
    public ICollection<Message> Messages { get; set; }

    /// <summary>Gets or sets the chat participants.</summary>
    public ICollection<User> Participants { get; set; }
}
