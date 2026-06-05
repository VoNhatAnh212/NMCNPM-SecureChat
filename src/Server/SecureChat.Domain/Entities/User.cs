#nullable enable

namespace SecureChat.Domain.Entities;

/// <summary>
/// Represents a registered SecureChat user.
/// </summary>
public class User
{
    /// <summary>
    /// Initializes a new instance of the <see cref="User"/> class.
    /// </summary>
    public User()
    {
        Devices = new HashSet<Device>();
        PublicKeys = new HashSet<PublicKey>();
        SentMessages = new HashSet<Message>();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="User"/> class with required fields.
    /// </summary>
    /// <param name="email">The user's email address.</param>
    /// <param name="displayName">The display name shown to other users.</param>
    public User(string email, string displayName)
        : this()
    {
        Id = Guid.NewGuid();
        Email = (email ?? string.Empty).Trim().ToLowerInvariant();
        DisplayName = (displayName ?? string.Empty).Trim();
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = CreatedAt;
    }

    /// <summary>Gets or sets the user identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the user's email address.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Gets or sets the user's display name.</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Gets or sets the user's avatar URL.</summary>
    public string AvatarUrl { get; set; } = string.Empty;

    /// <summary>Gets or sets the server-side password verifier derived from the client password hash.</summary>
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>Gets or sets the salt used for the server-side password verifier.</summary>
    public string PasswordSalt { get; set; } = string.Empty;

    /// <summary>Gets or sets a value indicating whether the user is online.</summary>
    public bool IsOnline { get; set; }

    /// <summary>Gets or sets the last time the user was seen online.</summary>
    public DateTimeOffset? LastSeen { get; set; }

    /// <summary>Gets or sets the time when the user was created.</summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>Gets or sets the time when the user was last updated.</summary>
    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>Gets or sets the user's registered devices.</summary>
    public ICollection<Device> Devices { get; set; }

    /// <summary>Gets or sets the user's public keys.</summary>
    public ICollection<PublicKey> PublicKeys { get; set; }

    /// <summary>Gets or sets messages sent by the user.</summary>
    public ICollection<Message> SentMessages { get; set; }
}
