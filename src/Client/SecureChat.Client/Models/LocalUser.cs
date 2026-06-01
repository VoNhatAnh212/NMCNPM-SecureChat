#nullable enable

namespace SecureChat.Client.Models;

/// <summary>
/// Represents a local account that has logged into the client.
/// </summary>
public class LocalUser
{
    /// <summary>Gets or sets the user identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the display name.</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Gets or sets the email address.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Gets or sets the avatar URL.</summary>
    public string AvatarUrl { get; set; } = string.Empty;

    /// <summary>Gets or sets the local device identifier.</summary>
    public string DeviceId { get; set; } = string.Empty;

    /// <summary>Gets or sets the local device name.</summary>
    public string DeviceName { get; set; } = string.Empty;

    /// <summary>Gets or sets the last login time.</summary>
    public DateTime LastLoginAt { get; set; }
}
