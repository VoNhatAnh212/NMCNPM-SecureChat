#nullable enable

using SecureChat.Shared.Enums;

namespace SecureChat.Domain.Entities;

/// <summary>
/// Represents an end-to-end encrypted message.
/// </summary>
public class Message
{
    /// <summary>Initializes a new instance of the <see cref="Message"/> class.</summary>
    public Message()
    {
        Attachments = new HashSet<Attachment>();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Message"/> class with required fields.
    /// </summary>
    /// <param name="chatId">The chat identifier.</param>
    /// <param name="senderId">The sender user identifier.</param>
    /// <param name="encryptedContent">The encrypted content bytes.</param>
    /// <param name="messageType">The message type.</param>
    /// <param name="iv">The encryption initialization vector.</param>
    /// <param name="authTag">The authentication tag.</param>
    public Message(Guid chatId, Guid senderId, byte[] encryptedContent, MessageType messageType, byte[] iv, byte[] authTag)
        : this()
    {
        Id = Guid.NewGuid();
        ChatId = chatId;
        SenderId = senderId;
        EncryptedContent = encryptedContent;
        MessageType = messageType;
        Status = MessageStatus.Sending;
        IV = iv;
        AuthTag = authTag;
        SentAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Gets or sets the message identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the chat identifier.</summary>
    public Guid ChatId { get; set; }

    /// <summary>Gets or sets the sender user identifier.</summary>
    public Guid SenderId { get; set; }

    /// <summary>Gets or sets the encrypted content bytes.</summary>
    public byte[] EncryptedContent { get; set; } = Array.Empty<byte>();

    /// <summary>Gets or sets the message type.</summary>
    public MessageType MessageType { get; set; }

    /// <summary>Gets or sets the message status.</summary>
    public MessageStatus Status { get; set; }

    /// <summary>Gets or sets the encryption initialization vector.</summary>
    public byte[] IV { get; set; } = Array.Empty<byte>();

    /// <summary>Gets or sets the authentication tag.</summary>
    public byte[] AuthTag { get; set; } = Array.Empty<byte>();

    /// <summary>Gets or sets the time when the message was sent.</summary>
    public DateTimeOffset SentAt { get; set; }

    /// <summary>Gets or sets the time when the message was delivered.</summary>
    public DateTimeOffset? DeliveredAt { get; set; }

    /// <summary>Gets or sets the time when the message was read.</summary>
    public DateTimeOffset? ReadAt { get; set; }

    /// <summary>Gets or sets a value indicating whether the message was deleted.</summary>
    public bool IsDeleted { get; set; }

    /// <summary>Gets or sets the chat.</summary>
    public Chat Chat { get; set; } = null!;

    /// <summary>Gets or sets the sender.</summary>
    public User Sender { get; set; } = null!;

    /// <summary>Gets or sets the message attachments.</summary>
    public ICollection<Attachment> Attachments { get; set; }
}
