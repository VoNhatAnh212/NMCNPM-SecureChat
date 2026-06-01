#nullable enable

using System.Media;

namespace SecureChat.Client.Forms.Helpers;

/// <summary>
/// Provides lightweight desktop notification helpers.
/// </summary>
public static class NotificationHelper
{
    private static NotifyIcon? _notifyIcon;

    /// <summary>
    /// Shows a toast notification using the Windows notification tray.
    /// </summary>
    /// <param name="title">The toast title.</param>
    /// <param name="message">The toast message.</param>
    public static void ShowToast(string title, string message)
    {
        _notifyIcon ??= new NotifyIcon
        {
            Icon = SystemIcons.Application,
            Visible = true,
            Text = "SecureChat"
        };

        _notifyIcon.BalloonTipTitle = title;
        _notifyIcon.BalloonTipText = message;
        _notifyIcon.ShowBalloonTip(3000);
    }

    /// <summary>
    /// Plays the default message sound.
    /// </summary>
    public static void PlayMessageSound()
    {
        SystemSounds.Asterisk.Play();
    }

    /// <summary>
    /// Updates the friend request badge placeholder.
    /// </summary>
    /// <param name="count">The request count.</param>
    public static void ShowFriendRequestBadge(int count)
    {
        if (count > 0)
        {
            ShowToast("SecureChat", $"{count} pending friend request(s).");
        }
    }
}
