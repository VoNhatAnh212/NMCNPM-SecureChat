#nullable enable

using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using SecureChat.Client.Controllers;
using SecureChat.Client.Forms.Auth;
using SecureChat.Client.Forms.Device;
using SecureChat.Client.Forms.Helpers;
using SecureChat.Client.Forms.Security;
using SecureChat.Client.Models;
using SecureChat.Client.Services;
using SecureChat.Client.Services.Api;
using SecureChat.Client.Services.Crypto;
using SecureChat.Client.Services.SignalR;

namespace SecureChat.Client.Forms.Settings;

/// <summary>
/// Provides SecureChat user settings and security shortcuts.
/// </summary>
public partial class SettingsForm : Form
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly IServiceProvider _serviceProvider;
    private readonly string _settingsPath;
    private AppSettings _settings = new();
    private bool _loading;

    /// <summary>Initializes a new instance of the <see cref="SettingsForm"/> class.</summary>
    public SettingsForm(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _settingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SecureChat", "AppSettings.json");
        InitializeComponent();
        LoadSettings();
        ApplySettingsToUi();
        ShowPage(_generalPage);
    }

    private void LoadSettings()
    {
        try
        {
            if (File.Exists(_settingsPath))
            {
                var json = File.ReadAllText(_settingsPath);
                _settings = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            _settings = new AppSettings();
        }
    }

    private void SaveSettings()
    {
        if (_loading)
        {
            return;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(_settingsPath)!);
        _settings.Theme = _darkThemeRadio.Checked ? "Dark" : "Light";
        _settings.Language = _languageCombo.SelectedItem?.ToString() ?? "EN";
        _settings.StartWithWindows = _startWithWindowsCheck.Checked;
        _settings.MinimizeToTray = _minimizeToTrayCheck.Checked;
        _settings.MaskOffensiveLanguage = _maskOffensiveLanguageCheck.Checked;
        _settings.EnableNotifications = _enableNotificationsCheck.Checked;
        _settings.MessageSound = _soundCombo.SelectedItem?.ToString() ?? "Default";
        _settings.GroupNotifications = _groupMessagesCheck.Checked;
        _settings.FriendNotifications = _friendRequestsCheck.Checked;
        File.WriteAllText(_settingsPath, JsonSerializer.Serialize(_settings, JsonOptions));
    }

    private async void SettingsFormLoad(object? sender, EventArgs e)
    {
        await LoadCurrentDeviceLabelAsync();
    }

    private async Task LoadCurrentDeviceLabelAsync()
    {
        var session = _serviceProvider.GetRequiredService<AuthSessionService>();
        var tokens = session.GetTokens();
        var deviceId = ExtractStringClaim(tokens.access, "deviceId");
        var accounts = await _serviceProvider.GetRequiredService<KeyStorageService>().LoadAccountsAsync();
        var account = session.CurrentAccount ?? accounts.FirstOrDefault(local => string.Equals(local.DeviceId, deviceId, StringComparison.OrdinalIgnoreCase));
        _currentDeviceLabel.Text = $"Current Device: {account?.DeviceName ?? Truncate(deviceId, 16) ?? "Unknown"}";
    }

    private void ApplySettingsToUi()
    {
        _loading = true;
        _darkThemeRadio.Checked = !string.Equals(_settings.Theme, "Light", StringComparison.OrdinalIgnoreCase);
        _lightThemeRadio.Checked = !_darkThemeRadio.Checked;
        _languageCombo.SelectedItem = _settings.Language;
        if (_languageCombo.SelectedIndex < 0)
        {
            _languageCombo.SelectedIndex = 0;
        }

        _startWithWindowsCheck.Checked = _settings.StartWithWindows;
        _minimizeToTrayCheck.Checked = _settings.MinimizeToTray;
        _maskOffensiveLanguageCheck.Checked = _settings.MaskOffensiveLanguage;
        _enableNotificationsCheck.Checked = _settings.EnableNotifications;
        _soundCombo.SelectedItem = _settings.MessageSound;
        if (_soundCombo.SelectedIndex < 0)
        {
            _soundCombo.SelectedIndex = 0;
        }

        _groupMessagesCheck.Checked = _settings.GroupNotifications;
        _friendRequestsCheck.Checked = _settings.FriendNotifications;
        _loading = false;
        ApplyThemeChoice();
    }

    private void NavButtonClick(object? sender, EventArgs e)
    {
        if (sender == _generalNavButton)
        {
            ShowPage(_generalPage);
        }
        else if (sender == _notificationsNavButton)
        {
            ShowPage(_notificationsPage);
        }
        else if (sender == _securityNavButton)
        {
            ShowPage(_securityPage);
        }
        else
        {
            ShowPage(_aboutPage);
        }
    }

    private void ShowPage(Panel page)
    {
        foreach (var control in _contentPanel.Controls.OfType<Panel>())
        {
            control.Visible = control == page;
        }
    }

    private void SettingChanged(object? sender, EventArgs e)
    {
        ApplyThemeChoice();
        SaveSettings();
    }

    private void ApplyThemeChoice()
    {
        if (_darkThemeRadio.Checked)
        {
            ThemeHelper.UseDarkTheme();
        }
        else
        {
            ThemeHelper.UseLightTheme();
        }

        ThemeHelper.ApplyThemeToOpenForms();
        if (_logoutButton is not null)
        {
            _logoutButton.BackColor = ThemeHelper.Danger;
        }

        if (_logoutAllButton is not null)
        {
            _logoutAllButton.BackColor = ThemeHelper.Danger;
        }
    }

    private void ManageDevicesButtonClick(object? sender, EventArgs e)
    {
        using var form = new DeviceManagementForm(_serviceProvider);
        form.ShowDialog(this);
    }

    private void SecurityCenterButtonClick(object? sender, EventArgs e)
    {
        using var form = new SecurityCenterForm(_serviceProvider);
        form.ShowDialog(this);
    }

    private async void LogoutButtonClick(object? sender, EventArgs e)
    {
        if (MessageBox.Show(this, "Logout this device?", "Logout", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
        {
            return;
        }

        await LogoutLocalAsync(callServer: true);
    }

    private async void LogoutAllButtonClick(object? sender, EventArgs e)
    {
        if (MessageBox.Show(this, "Logout all devices?", "Logout all devices", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
        {
            return;
        }

        await _serviceProvider.GetRequiredService<AuthApiService>().LogoutAllDevicesAsync();
        await LogoutLocalAsync(callServer: false);
    }

    private async Task LogoutLocalAsync(bool callServer)
    {
        try
        {
            if (callServer)
            {
                await _serviceProvider.GetRequiredService<AuthController>().LogoutAsync();
            }
            else
            {
                await _serviceProvider.GetRequiredService<ISignalRService>().DisconnectAsync();
                var session = _serviceProvider.GetRequiredService<AuthSessionService>();
                var account = session.CurrentAccount;
                session.Clear();
                await _serviceProvider.GetRequiredService<KeyStorageService>().ClearTokensAsync(account);
            }
        }
        catch
        {
            await _serviceProvider.GetRequiredService<ISignalRService>().DisconnectAsync();
            var session = _serviceProvider.GetRequiredService<AuthSessionService>();
            var account = session.CurrentAccount;
            session.Clear();
            await _serviceProvider.GetRequiredService<KeyStorageService>().ClearTokensAsync(account);
        }

        var owner = Owner;
        Hide();
        owner?.Hide();
        var loginForm = new LoginForm(_serviceProvider);
        loginForm.FormClosed += (_, _) => owner?.Close();
        loginForm.Show();
        Close();
    }

    private static string? ExtractStringClaim(string? accessToken, string claimName)
    {
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return null;
        }

        var parts = accessToken.Split('.');
        if (parts.Length < 2)
        {
            return null;
        }

        try
        {
            var payload = parts[1].Replace('-', '+').Replace('_', '/');
            payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');
            var json = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(payload));
            using var document = JsonDocument.Parse(json);
            return document.RootElement.TryGetProperty(claimName, out var value) ? value.GetString() : null;
        }
        catch (Exception ex) when (ex is FormatException or JsonException)
        {
            return null;
        }
    }

    private static string? Truncate(string? value, int length)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Length <= length ? value : $"{value[..length]}...";
    }
}
