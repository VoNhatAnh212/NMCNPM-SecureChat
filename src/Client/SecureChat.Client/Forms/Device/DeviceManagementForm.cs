#nullable enable

using Microsoft.Extensions.DependencyInjection;
using SecureChat.Client.Forms.Helpers;
using SecureChat.Client.Services.Api;
using SecureChat.Shared.DTOs.Device;
using SecureChat.Shared.Enums;

namespace SecureChat.Client.Forms.Device;

/// <summary>
/// Displays and manages trusted devices for the current account.
/// </summary>
public partial class DeviceManagementForm : Form
{
    private readonly AuthApiService _authApiService;
    private readonly List<DeviceDto> _devices = new();
    private readonly Dictionary<Guid, Rectangle> _revokeButtonBounds = new();

    /// <summary>Initializes a new instance of the <see cref="DeviceManagementForm"/> class.</summary>
    public DeviceManagementForm(IServiceProvider serviceProvider)
    {
        _authApiService = serviceProvider.GetRequiredService<AuthApiService>();
        InitializeComponent();
    }

    private async void DeviceManagementFormLoad(object? sender, EventArgs e)
    {
        await RefreshDevicesAsync();
    }

    private async Task RefreshDevicesAsync()
    {
        _statusLabel.Text = "Loading devices...";
        _revokeButtonBounds.Clear();
        _devices.Clear();
        _deviceList.Items.Clear();

        try
        {
            _devices.AddRange(await _authApiService.GetMyDevicesAsync());
            foreach (var device in _devices)
            {
                _deviceList.Items.Add(new ListViewItem { Tag = device });
            }

            _statusLabel.Text = $"{_devices.Count} device(s)";
        }
        catch (Exception ex)
        {
            _statusLabel.Text = ex.Message;
        }
    }

    private void DeviceListDrawItem(object? sender, DrawListViewItemEventArgs e)
    {
        if (e.Item.Tag is not DeviceDto device)
        {
            return;
        }

        var g = e.Graphics;
        var bounds = new Rectangle(e.Bounds.Left + 10, e.Bounds.Top + 8, e.Bounds.Width - 20, e.Bounds.Height - 12);
        using var cardBrush = new SolidBrush(ThemeHelper.BgCard);
        using var borderPen = new Pen(ThemeHelper.Border);
        using var titleBrush = new SolidBrush(ThemeHelper.TextPrimary);
        using var mutedBrush = new SolidBrush(ThemeHelper.TextSecondary);
        using var badgeBrush = new SolidBrush(GetBadgeColor(device));
        using var badgeTextBrush = new SolidBrush(Color.White);
        using var titleFont = new Font("Segoe UI", 10.5F, FontStyle.Bold);
        using var smallFont = new Font("Segoe UI", 8.5F, FontStyle.Regular);

        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        using var path = ThemeHelper.CreateRoundedRectanglePath(bounds, 8);
        g.FillPath(cardBrush, path);
        g.DrawPath(borderPen, path);

        var icon = IsMobile(device.Platform) ? "📱" : "💻";
        TextRenderer.DrawText(g, icon, new Font("Segoe UI Emoji", 20F), new Rectangle(bounds.Left + 16, bounds.Top + 18, 38, 38), ThemeHelper.TextPrimary);

        var textLeft = bounds.Left + 62;
        TextRenderer.DrawText(g, device.DeviceName, titleFont, new Rectangle(textLeft, bounds.Top + 14, bounds.Width - 190, 24), ThemeHelper.TextPrimary, TextFormatFlags.EndEllipsis);
        TextRenderer.DrawText(g, $"{device.Platform} • {Truncate(device.Fingerprint, 18)}", smallFont, new Rectangle(textLeft, bounds.Top + 40, bounds.Width - 190, 20), ThemeHelper.TextSecondary, TextFormatFlags.EndEllipsis);
        var lastLogin = device.LastLoginAt.HasValue ? DateTimeHelper.ToRelativeString(device.LastLoginAt.Value.LocalDateTime) : "never";
        TextRenderer.DrawText(g, $"Last login: {lastLogin}", smallFont, new Rectangle(textLeft, bounds.Top + 61, bounds.Width - 190, 20), ThemeHelper.TextMuted, TextFormatFlags.EndEllipsis);

        var badgeText = device.IsCurrentDevice ? "This Device" : device.Status == DeviceStatus.Revoked ? "Revoked" : "Active";
        var badgeRect = new Rectangle(bounds.Right - 142, bounds.Top + 16, 112, 24);
        using (var badgePath = ThemeHelper.CreateRoundedRectanglePath(badgeRect, 12))
        {
            g.FillPath(badgeBrush, badgePath);
        }

        TextRenderer.DrawText(g, badgeText, smallFont, badgeRect, Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

        var revokeRect = new Rectangle(bounds.Right - 120, bounds.Bottom - 38, 90, 28);
        _revokeButtonBounds[device.Id] = revokeRect;
        using var revokeBrush = new SolidBrush(device.IsCurrentDevice || device.Status == DeviceStatus.Revoked ? ThemeHelper.BgHover : ThemeHelper.Danger);
        using (var revokePath = ThemeHelper.CreateRoundedRectanglePath(revokeRect, 6))
        {
            g.FillPath(revokeBrush, revokePath);
        }

        TextRenderer.DrawText(g, "Revoke", smallFont, revokeRect, device.IsCurrentDevice ? ThemeHelper.TextMuted : Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
    }

    private async void DeviceListMouseClick(object? sender, MouseEventArgs e)
    {
        var device = _devices.FirstOrDefault(item => _revokeButtonBounds.TryGetValue(item.Id, out var rect) && rect.Contains(e.Location));
        if (device is null || device.IsCurrentDevice || device.Status == DeviceStatus.Revoked)
        {
            return;
        }

        var result = MessageBox.Show(this, $"Revoke {device.DeviceName}?", "Revoke device", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
        if (result != DialogResult.Yes)
        {
            return;
        }

        await _authApiService.RevokeDeviceAsync(device.Id);
        await RefreshDevicesAsync();
    }

    private async void RefreshButtonClick(object? sender, EventArgs e)
    {
        await RefreshDevicesAsync();
    }

    private static Color GetBadgeColor(DeviceDto device)
    {
        if (device.IsCurrentDevice)
        {
            return ThemeHelper.AccentGreen;
        }

        return device.Status == DeviceStatus.Revoked ? ThemeHelper.TextMuted : ThemeHelper.AccentBlue;
    }

    private static bool IsMobile(string platform)
    {
        return platform.Contains("android", StringComparison.OrdinalIgnoreCase)
            || platform.Contains("ios", StringComparison.OrdinalIgnoreCase)
            || platform.Contains("mobile", StringComparison.OrdinalIgnoreCase);
    }

    private static string Truncate(string value, int length)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "unknown";
        }

        return value.Length <= length ? value : $"{value[..length]}...";
    }
}
