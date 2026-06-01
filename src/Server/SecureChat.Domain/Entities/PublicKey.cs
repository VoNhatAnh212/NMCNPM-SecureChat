#nullable enable

namespace SecureChat.Domain.Entities;

/// <summary>
/// Represents public key material associated with a user device.
/// </summary>
public class PublicKey
{
    /// <summary>Initializes a new instance of the <see cref="PublicKey"/> class.</summary>
    public PublicKey()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PublicKey"/> class with required fields.
    /// </summary>
    /// <param name="userId">The owning user identifier.</param>
    /// <param name="deviceId">The owning device identifier.</param>
    /// <param name="publicKeyPem">The PEM encoded public key.</param>
    /// <param name="keyFingerprint">The public key fingerprint.</param>
    public PublicKey(Guid userId, Guid deviceId, string publicKeyPem, string keyFingerprint)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        DeviceId = deviceId;
        PublicKeyPem = publicKeyPem;
        KeyFingerprint = keyFingerprint;
        IsActive = true;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Gets or sets the public key identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the owning user identifier.</summary>
    public Guid UserId { get; set; }

    /// <summary>Gets or sets the owning device identifier.</summary>
    public Guid DeviceId { get; set; }

    /// <summary>Gets or sets the PEM encoded public key.</summary>
    public string PublicKeyPem { get; set; } = string.Empty;

    /// <summary>Gets or sets the public key fingerprint.</summary>
    public string KeyFingerprint { get; set; } = string.Empty;

    /// <summary>Gets or sets a value indicating whether the key is active.</summary>
    public bool IsActive { get; set; }

    /// <summary>Gets or sets the time when the key was created.</summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>Gets or sets the owning user.</summary>
    public User User { get; set; } = null!;

    /// <summary>Gets or sets the owning device.</summary>
    public Device Device { get; set; } = null!;
}
