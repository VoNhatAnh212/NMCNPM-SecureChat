#nullable enable

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using SecureChat.Client.Forms.Helpers;
using SecureChat.Client.Services;
using SecureChat.Client.Services.Api;
using SecureChat.Client.Services.Crypto;
using SecureChat.Client.Services.SignalR;
using SecureChat.Client.Services.Storage;
using SecureChat.Shared.DTOs.Device;

namespace SecureChat.Client.Forms.Security;

/// <summary>
/// Shows identity, device, and key management controls for the current account.
/// </summary>
public partial class SecurityCenterForm : Form
{
    private readonly IServiceProvider _serviceProvider;
    private readonly AuthApiService _authApiService;
    private readonly KeyStorageService _keyStorageService;
    private readonly AuthSessionService _authSession;
    private readonly ISignalRService _signalRService;
    private readonly RsaService _rsaService;
    private readonly SessionCache _sessionCache;
    private string? _currentDeviceId;
    private string? _currentPrivateKeyPem;
    private string? _currentPublicKeyPem;
    private string _currentFingerprint = string.Empty;

    /// <summary>Initializes a new instance of the <see cref="SecurityCenterForm"/> class.</summary>
    public SecurityCenterForm(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _authApiService = serviceProvider.GetRequiredService<AuthApiService>();
        _keyStorageService = serviceProvider.GetRequiredService<KeyStorageService>();
        _authSession = serviceProvider.GetRequiredService<AuthSessionService>();
        _signalRService = serviceProvider.GetRequiredService<ISignalRService>();
        _rsaService = serviceProvider.GetRequiredService<RsaService>();
        _sessionCache = serviceProvider.GetRequiredService<SessionCache>();
        InitializeComponent();
    }

    private async void SecurityCenterFormLoad(object? sender, EventArgs e)
    {
        await LoadSecurityStateAsync();
    }

    private async Task LoadSecurityStateAsync()
    {
        var tokens = _authSession.GetTokens();
        _currentDeviceId = ExtractStringClaim(tokens.access, "deviceId");
        var currentUserId = ExtractGuidClaim(tokens.access, "sub");
        var accounts = await _keyStorageService.LoadAccountsAsync();
        var account = accounts.FirstOrDefault(local =>
                string.Equals(local.DeviceId, _currentDeviceId, StringComparison.OrdinalIgnoreCase)
                || (currentUserId.HasValue && local.Id == currentUserId.Value))
            ?? accounts.OrderByDescending(local => local.LastLoginAt).FirstOrDefault();

        _displayNameLabel.Text = account?.DisplayName ?? "SecureChat User";
        _avatarBox.Image?.Dispose();
        _avatarBox.Image = ImageHelper.GenerateAvatarFromInitials(_displayNameLabel.Text, 72);

        if (!string.IsNullOrWhiteSpace(_currentDeviceId))
        {
            _currentPrivateKeyPem = await _keyStorageService.LoadPrivateKeyAsync(_currentDeviceId);
        }

        if (!string.IsNullOrWhiteSpace(_currentPrivateKeyPem))
        {
            _currentPublicKeyPem = await _rsaService.ExportPublicKeyAsync(_currentPrivateKeyPem);
            _currentFingerprint = await _rsaService.GetPublicKeyFingerprintAsync(_currentPublicKeyPem);
        }

        RenderFingerprint(_currentFingerprint);

        var devices = await TryGetDevicesAsync();
        var currentDevice = devices.FirstOrDefault(device => device.IsCurrentDevice)
            ?? devices.FirstOrDefault(device => string.Equals(device.Id.ToString(), _currentDeviceId, StringComparison.OrdinalIgnoreCase));
        _deviceIdLabel.Text = $"Device ID: {Truncate(_currentDeviceId, 18)}";
        _deviceNameLabel.Text = $"Device Name: {currentDevice?.DeviceName ?? account?.DeviceName ?? Environment.MachineName}";
        _createdAtLabel.Text = currentDevice is null ? "Created At: Unknown" : $"Created At: {DateTimeHelper.ToDisplayDate(currentDevice.CreatedAt.LocalDateTime)}";
        _lastLoginLabel.Text = currentDevice?.LastLoginAt is null ? "Last Login: Unknown" : $"Last Login: {DateTimeHelper.ToRelativeString(currentDevice.LastLoginAt.Value.LocalDateTime)}";
        UpdateJwtStatus(tokens.expiry);
    }

    private void RenderFingerprint(string fingerprint)
    {
        _fingerprintPanel.Controls.Clear();
        if (string.IsNullOrWhiteSpace(fingerprint))
        {
            _fingerprintPanel.Controls.Add(new Label
            {
                Text = "Unavailable",
                AutoSize = true,
                ForeColor = ThemeHelper.TextMuted,
                Font = new Font("Consolas", 16F)
            });
            return;
        }

        foreach (var part in fingerprint.Split(':', StringSplitOptions.RemoveEmptyEntries))
        {
            var label = new Label
            {
                Text = part,
                AutoSize = true,
                Margin = new Padding(0, 0, 2, 2),
                Font = new Font("Consolas", 16F, FontStyle.Bold),
                ForeColor = ByteColor(part)
            };
            _fingerprintPanel.Controls.Add(label);
            _fingerprintPanel.Controls.Add(new Label
            {
                Text = ":",
                AutoSize = true,
                Margin = new Padding(0, 0, 2, 2),
                Font = new Font("Consolas", 16F),
                ForeColor = ThemeHelper.TextMuted
            });
        }

        if (_fingerprintPanel.Controls.Count > 0)
        {
            _fingerprintPanel.Controls.RemoveAt(_fingerprintPanel.Controls.Count - 1);
        }
    }

    private async Task<List<DeviceDto>> TryGetDevicesAsync()
    {
        try
        {
            return await _authApiService.GetMyDevicesAsync();
        }
        catch
        {
            return new List<DeviceDto>();
        }
    }

    private void UpdateJwtStatus(DateTime? expiry)
    {
        if (expiry is null || expiry.Value <= DateTime.UtcNow)
        {
            _jwtStatusLabel.Text = "JWT Status: Expired";
            _jwtStatusLabel.ForeColor = ThemeHelper.Danger;
            return;
        }

        var minutes = Math.Max(1, (int)Math.Ceiling((expiry.Value.ToUniversalTime() - DateTime.UtcNow).TotalMinutes));
        _jwtStatusLabel.Text = $"JWT Status: Valid (expires in {minutes} minutes)";
        _jwtStatusLabel.ForeColor = ThemeHelper.AccentGreen;
    }

    private void CopyFingerprintButtonClick(object? sender, EventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(_currentFingerprint))
        {
            Clipboard.SetText(_currentFingerprint);
        }
    }

    private void ExportPublicKeyButtonClick(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_currentPublicKeyPem))
        {
            MessageBox.Show(this, "Public key is unavailable.", "Export public key", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        using var dialog = new SaveFileDialog
        {
            Filter = "PEM public key (*.pem)|*.pem",
            FileName = "securechat-public-key.pem"
        };
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            File.WriteAllText(dialog.FileName, _currentPublicKeyPem);
        }
    }

    private async void RefreshTokenButtonClick(object? sender, EventArgs e)
    {
        _refreshTokenButton.Enabled = false;
        var originalText = _refreshTokenButton.Text;
        _refreshTokenButton.Text = "Refreshing...";
        try
        {
            var account = _authSession.CurrentAccount;
            var tokens = _authSession.GetTokens();
            if (string.IsNullOrWhiteSpace(tokens.refresh))
            {
                tokens = await _keyStorageService.LoadTokensAsync(account);
            }

            if (string.IsNullOrWhiteSpace(tokens.refresh))
            {
                throw new InvalidOperationException("No refresh token is available.");
            }

            var response = await _authApiService.RefreshTokenAsync(tokens.refresh);
            _authSession.UpdateTokens(response.AccessToken, response.RefreshToken, response.ExpiresAt.DateTime);
            await _keyStorageService.SaveTokensAsync(response.AccessToken, response.RefreshToken, response.ExpiresAt.DateTime, account);

            if (_signalRService.IsConnected)
            {
                await _signalRService.DisconnectAsync();
                await _signalRService.ConnectAsync(response.AccessToken);
            }

            UpdateJwtStatus(response.ExpiresAt.DateTime);
            await LoadSecurityStateAsync();
            MessageBox.Show(this, "Token refreshed.", "Security Center", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Refresh token failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _refreshTokenButton.Text = originalText;
            _refreshTokenButton.Enabled = true;
        }
    }

    private async void RegenerateKeyButtonClick(object? sender, EventArgs e)
    {
        var result = MessageBox.Show(
            this,
            "Regenerating your RSA key pair logs out old sessions for encrypted chats. All chat session keys will be exchanged again.",
            "Regenerate RSA key pair",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);
        if (result != DialogResult.Yes)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(_currentDeviceId))
        {
            MessageBox.Show(this, "Current device id is unavailable.", "Security Center", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        try
        {
            var keyPair = await _rsaService.GenerateKeyPairAsync();
            var fingerprint = await _rsaService.GetPublicKeyFingerprintAsync(keyPair.publicKeyPem);
            await _authApiService.RotateCurrentDeviceKeyAsync(new RegisterDeviceDto
            {
                DeviceName = Environment.MachineName,
                Platform = Environment.OSVersion.Platform.ToString(),
                PublicKeyPem = keyPair.publicKeyPem,
                DeviceFingerprint = fingerprint
            });

            await _keyStorageService.SavePrivateKeyAsync(_currentDeviceId, keyPair.privateKeyPem);
            _sessionCache.ClearAll();
            _currentPrivateKeyPem = keyPair.privateKeyPem;
            _currentPublicKeyPem = keyPair.publicKeyPem;
            _currentFingerprint = fingerprint;
            RenderFingerprint(_currentFingerprint);
            MessageBox.Show(this, "RSA key pair regenerated. Session keys will be re-exchanged on next message.", "Security Center", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Regenerate key failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async void LogoutAllDevicesButtonClick(object? sender, EventArgs e)
    {
        if (MessageBox.Show(this, "Logout all devices?", "Logout all devices", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
        {
            return;
        }

        await _authApiService.LogoutAllDevicesAsync();
        _sessionCache.ClearAll();
        var account = _authSession.CurrentAccount;
        _authSession.Clear();
        await _keyStorageService.ClearTokensAsync(account);
        MessageBox.Show(this, "All devices have been logged out. Please sign in again.", "Security Center", MessageBoxButtons.OK, MessageBoxIcon.Information);
        Close();
    }

    private void ExportKeyBackupButtonClick(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_currentPrivateKeyPem))
        {
            MessageBox.Show(this, "Private key is unavailable.", "Export key backup", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var passphrase = PromptPassphrase();
        if (string.IsNullOrWhiteSpace(passphrase))
        {
            return;
        }

        using var dialog = new SaveFileDialog
        {
            Filter = "SecureChat key backup (*.sckey)|*.sckey",
            FileName = "securechat-key-backup.sckey"
        };
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        var backup = EncryptPrivateKeyBackup(_currentPrivateKeyPem, passphrase);
        File.WriteAllText(dialog.FileName, JsonSerializer.Serialize(backup, new JsonSerializerOptions { WriteIndented = true }));
    }

    private static KeyBackupDto EncryptPrivateKeyBackup(string privateKeyPem, string passphrase)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var nonce = RandomNumberGenerator.GetBytes(12);
        var plaintext = Encoding.UTF8.GetBytes(privateKeyPem);
        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[16];
        var key = Rfc2898DeriveBytes.Pbkdf2(passphrase, salt, 200_000, HashAlgorithmName.SHA256, 32);
        using var aes = new AesGcm(key, 16);
        aes.Encrypt(nonce, plaintext, ciphertext, tag);
        CryptographicOperations.ZeroMemory(key);
        CryptographicOperations.ZeroMemory(plaintext);

        return new KeyBackupDto
        {
            Version = 1,
            Algorithm = "PBKDF2-SHA256 + AES-256-GCM",
            Salt = Convert.ToBase64String(salt),
            Nonce = Convert.ToBase64String(nonce),
            Tag = Convert.ToBase64String(tag),
            Ciphertext = Convert.ToBase64String(ciphertext)
        };
    }

    private static string? PromptPassphrase()
    {
        using var form = new Form
        {
            Text = "Key backup passphrase",
            Size = new Size(360, 160),
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false
        };
        var input = new TextBox { UseSystemPasswordChar = true, Dock = DockStyle.Top, Margin = new Padding(12) };
        var label = new Label { Text = "Enter a passphrase for the encrypted backup:", Dock = DockStyle.Top, Height = 34 };
        var button = new Button { Text = "Export", Dock = DockStyle.Bottom, Height = 34, DialogResult = DialogResult.OK };
        form.Controls.Add(input);
        form.Controls.Add(label);
        form.Controls.Add(button);
        form.AcceptButton = button;
        return form.ShowDialog() == DialogResult.OK ? input.Text : null;
    }

    private static Color ByteColor(string hex)
    {
        var value = Convert.ToInt32(hex, 16);
        return Color.FromArgb(255, 90 + value % 120, 90 + value * 3 % 120, 90 + value * 7 % 120);
    }

    private static Guid? ExtractGuidClaim(string? accessToken, string claimName)
    {
        var value = ExtractStringClaim(accessToken, claimName);
        return Guid.TryParse(value, out var id) ? id : null;
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
            var json = Encoding.UTF8.GetString(Convert.FromBase64String(payload));
            using var document = JsonDocument.Parse(json);
            return document.RootElement.TryGetProperty(claimName, out var value) ? value.GetString() : null;
        }
        catch (Exception ex) when (ex is FormatException or JsonException)
        {
            return null;
        }
    }

    private static string Truncate(string? value, int length)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "Unknown";
        }

        return value.Length <= length ? value : $"{value[..length]}...";
    }

    private sealed class KeyBackupDto
    {
        public int Version { get; init; }
        public string Algorithm { get; init; } = string.Empty;
        public string Salt { get; init; } = string.Empty;
        public string Nonce { get; init; } = string.Empty;
        public string Tag { get; init; } = string.Empty;
        public string Ciphertext { get; init; } = string.Empty;
    }
}
