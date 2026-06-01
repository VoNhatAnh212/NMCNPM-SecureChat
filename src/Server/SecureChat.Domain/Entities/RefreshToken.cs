#nullable enable

namespace SecureChat.Domain.Entities;

/// <summary>
/// Represents a refresh token issued to a user device.
/// </summary>
public class RefreshToken
{
    /// <summary>Initializes a new instance of the <see cref="RefreshToken"/> class.</summary>
    public RefreshToken()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RefreshToken"/> class with required fields.
    /// </summary>
    /// <param name="userId">The owning user identifier.</param>
    /// <param name="deviceId">The owning device identifier.</param>
    /// <param name="token">The refresh token value.</param>
    /// <param name="expiresAt">The expiration time.</param>
    public RefreshToken(Guid userId, Guid deviceId, string token, DateTimeOffset expiresAt)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        DeviceId = deviceId;
        Token = token;
        ExpiresAt = expiresAt;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Gets or sets the refresh token identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the owning user identifier.</summary>
    public Guid UserId { get; set; }

    /// <summary>Gets or sets the owning device identifier.</summary>
    public Guid DeviceId { get; set; }

    /// <summary>Gets or sets the refresh token value.</summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>Gets or sets the expiration time.</summary>
    public DateTimeOffset ExpiresAt { get; set; }

    /// <summary>Gets or sets a value indicating whether the token was revoked.</summary>
    public bool IsRevoked { get; set; }

    /// <summary>Gets or sets the time when the token was created.</summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>Gets or sets the time when the token was revoked.</summary>
    public DateTimeOffset? RevokedAt { get; set; }

    /// <summary>Gets or sets the owning user.</summary>
    public User User { get; set; } = null!;

    /// <summary>Gets or sets the owning device.</summary>
    public Device Device { get; set; } = null!;
}
