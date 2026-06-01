#nullable enable

using SecureChat.Shared.Enums;

namespace SecureChat.Domain.Entities;

/// <summary>
/// Represents a trusted device registered to a user.
/// </summary>
public class Device
{
    /// <summary>Initializes a new instance of the <see cref="Device"/> class.</summary>
    public Device()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Device"/> class with required fields.
    /// </summary>
    /// <param name="userId">The owning user identifier.</param>
    /// <param name="deviceName">The device name.</param>
    /// <param name="platform">The device platform.</param>
    /// <param name="deviceFingerprint">The unique device fingerprint.</param>
    public Device(Guid userId, string deviceName, string platform, string deviceFingerprint)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        DeviceName = deviceName;
        Platform = platform;
        DeviceFingerprint = deviceFingerprint;
        Status = DeviceStatus.Active;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Gets or sets the device identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the owning user identifier.</summary>
    public Guid UserId { get; set; }

    /// <summary>Gets or sets the device name.</summary>
    public string DeviceName { get; set; } = string.Empty;

    /// <summary>Gets or sets the platform name.</summary>
    public string Platform { get; set; } = string.Empty;

    /// <summary>Gets or sets the device fingerprint.</summary>
    public string DeviceFingerprint { get; set; } = string.Empty;

    /// <summary>Gets or sets the device status.</summary>
    public DeviceStatus Status { get; set; }

    /// <summary>Gets or sets the last login time.</summary>
    public DateTimeOffset? LastLoginAt { get; set; }

    /// <summary>Gets or sets the time when the device was created.</summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>Gets or sets the owning user.</summary>
    public User User { get; set; } = null!;

    /// <summary>Gets or sets the public key associated with the device.</summary>
    public PublicKey? PublicKey { get; set; }
}
