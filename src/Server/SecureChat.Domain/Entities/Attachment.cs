#nullable enable

namespace SecureChat.Domain.Entities;

/// <summary>
/// Represents an encrypted attachment stored for a message.
/// </summary>
public class Attachment
{
    /// <summary>Initializes a new instance of the <see cref="Attachment"/> class.</summary>
    public Attachment()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Attachment"/> class with required fields.
    /// </summary>
    /// <param name="messageId">The owning message identifier.</param>
    /// <param name="fileName">The original file name.</param>
    /// <param name="fileSize">The file size in bytes.</param>
    /// <param name="mimeType">The MIME type.</param>
    /// <param name="encryptedStoragePath">The encrypted storage path.</param>
    /// <param name="encryptedFileKey">The encrypted file key bytes.</param>
    /// <param name="iv">The initialization vector.</param>
    /// <param name="authTag">The authentication tag.</param>
    public Attachment(Guid messageId, string fileName, long fileSize, string mimeType, string encryptedStoragePath, byte[] encryptedFileKey, byte[] iv, byte[] authTag)
    {
        Id = Guid.NewGuid();
        MessageId = messageId;
        FileName = fileName;
        FileSize = fileSize;
        MimeType = mimeType;
        EncryptedStoragePath = encryptedStoragePath;
        EncryptedFileKey = encryptedFileKey;
        IV = iv;
        AuthTag = authTag;
        UploadedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Gets or sets the attachment identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the owning message identifier.</summary>
    public Guid? MessageId { get; set; }

    /// <summary>Gets or sets the original file name.</summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>Gets or sets the file size in bytes.</summary>
    public long FileSize { get; set; }

    /// <summary>Gets or sets the MIME type.</summary>
    public string MimeType { get; set; } = string.Empty;

    /// <summary>Gets or sets the encrypted storage path.</summary>
    public string EncryptedStoragePath { get; set; } = string.Empty;

    /// <summary>Gets or sets the encrypted file key bytes.</summary>
    public byte[] EncryptedFileKey { get; set; } = Array.Empty<byte>();

    /// <summary>Gets or sets the initialization vector.</summary>
    public byte[] IV { get; set; } = Array.Empty<byte>();

    /// <summary>Gets or sets the authentication tag.</summary>
    public byte[] AuthTag { get; set; } = Array.Empty<byte>();

    /// <summary>Gets or sets the time when the attachment was uploaded.</summary>
    public DateTimeOffset UploadedAt { get; set; }

    /// <summary>Gets or sets the owning message.</summary>
    public Message? Message { get; set; }
}
