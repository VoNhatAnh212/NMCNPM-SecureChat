#nullable enable

namespace SecureChat.Domain.Entities;

/// <summary>
/// Represents a per-user hidden chat state. The chat and participants remain intact.
/// </summary>
public class HiddenChat
{
    /// <summary>Initializes a new instance of the <see cref="HiddenChat"/> class.</summary>
    public HiddenChat()
    {
    }

    /// <summary>Initializes a new instance of the <see cref="HiddenChat"/> class.</summary>
    public HiddenChat(Guid chatId, Guid userId)
    {
        Id = Guid.NewGuid();
        ChatId = chatId;
        UserId = userId;
        HiddenAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Gets or sets the hidden chat identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the chat identifier.</summary>
    public Guid ChatId { get; set; }

    /// <summary>Gets or sets the user identifier.</summary>
    public Guid UserId { get; set; }

    /// <summary>Gets or sets the time when the chat was hidden.</summary>
    public DateTimeOffset HiddenAt { get; set; }

    /// <summary>Gets or sets the hidden chat.</summary>
    public Chat Chat { get; set; } = null!;

    /// <summary>Gets or sets the user who hid the chat.</summary>
    public User User { get; set; } = null!;
}
