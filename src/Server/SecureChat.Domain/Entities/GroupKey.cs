#nullable enable

namespace SecureChat.Domain.Entities;

/// <summary>
/// Represents an encrypted group key for a specific device.
/// </summary>
public class GroupKey
{
    /// <summary>Initializes a new instance of the <see cref="GroupKey"/> class.</summary>
    public GroupKey()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GroupKey"/> class with required fields.
    /// </summary>
    /// <param name="groupId">The group identifier.</param>
    /// <param name="encryptedGroupKey">The encrypted group key bytes.</param>
    /// <param name="forDeviceId">The recipient device identifier.</param>
    /// <param name="keyVersion">The group key version.</param>
    public GroupKey(Guid groupId, byte[] encryptedGroupKey, Guid forDeviceId, int keyVersion)
    {
        Id = Guid.NewGuid();
        GroupId = groupId;
        EncryptedGroupKey = encryptedGroupKey;
        ForDeviceId = forDeviceId;
        KeyVersion = keyVersion;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Gets or sets the group key identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the group identifier.</summary>
    public Guid GroupId { get; set; }

    /// <summary>Gets or sets the encrypted group key bytes.</summary>
    public byte[] EncryptedGroupKey { get; set; } = Array.Empty<byte>();

    /// <summary>Gets or sets the recipient device identifier.</summary>
    public Guid ForDeviceId { get; set; }

    /// <summary>Gets or sets the key version.</summary>
    public int KeyVersion { get; set; }

    /// <summary>Gets or sets the time when the group key was created.</summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>Gets or sets the group.</summary>
    public Group Group { get; set; } = null!;

    /// <summary>Gets or sets the recipient device.</summary>
    public Device Device { get; set; } = null!;
}
