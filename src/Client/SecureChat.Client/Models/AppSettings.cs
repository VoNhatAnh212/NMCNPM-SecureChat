#nullable enable

namespace SecureChat.Client.Models;

/// <summary>
/// Stores non-sensitive local application preferences.
/// </summary>
public class AppSettings
{
    /// <summary>Gets or sets the selected theme.</summary>
    public string Theme { get; set; } = "Dark";

    /// <summary>Gets or sets the selected language.</summary>
    public string Language { get; set; } = "EN";

    /// <summary>Gets or sets a value indicating whether desktop notifications are enabled.</summary>
    public bool EnableNotifications { get; set; } = true;

    /// <summary>Gets or sets the selected message sound.</summary>
    public string MessageSound { get; set; } = "Default";

    /// <summary>Gets or sets a value indicating whether group message notifications are enabled.</summary>
    public bool GroupNotifications { get; set; } = true;

    /// <summary>Gets or sets a value indicating whether friend request notifications are enabled.</summary>
    public bool FriendNotifications { get; set; } = true;

    /// <summary>Gets or sets a value indicating whether the app should minimize to the system tray.</summary>
    public bool MinimizeToTray { get; set; }

    /// <summary>Gets or sets a value indicating whether the app should start with Windows.</summary>
    public bool StartWithWindows { get; set; }
}
