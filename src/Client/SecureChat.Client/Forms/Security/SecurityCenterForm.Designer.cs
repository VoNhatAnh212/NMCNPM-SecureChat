#nullable enable

using SecureChat.Client.Forms.Helpers;

namespace SecureChat.Client.Forms.Security;

/// <summary>
/// Designer portion of <see cref="SecurityCenterForm"/>.
/// </summary>
public partial class SecurityCenterForm
{
    private Panel _scrollPanel = null!;
    private FlowLayoutPanel _contentPanel = null!;
    private PictureBox _avatarBox = null!;
    private Label _displayNameLabel = null!;
    private FlowLayoutPanel _fingerprintPanel = null!;
    private Button _copyFingerprintButton = null!;
    private Button _exportPublicKeyButton = null!;
    private Label _deviceIdLabel = null!;
    private Label _deviceNameLabel = null!;
    private Label _createdAtLabel = null!;
    private Label _lastLoginLabel = null!;
    private Label _jwtStatusLabel = null!;
    private Button _refreshTokenButton = null!;
    private Button _regenerateKeyButton = null!;
    private Button _logoutAllDevicesButton = null!;
    private Button _exportKeyBackupButton = null!;

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _avatarBox.Image?.Dispose();
        }

        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        SuspendLayout();

        _scrollPanel = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            BackColor = ThemeHelper.BgPrimary
        };

        _contentPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoSize = true,
            Padding = new Padding(18),
            BackColor = ThemeHelper.BgPrimary
        };

        BuildIdentityCard();
        BuildCurrentDeviceCard();
        BuildSecurityStatusCard();
        BuildDangerZoneCard();

        _scrollPanel.Controls.Add(_contentPanel);
        Controls.Add(_scrollPanel);

        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        BackColor = ThemeHelper.BgPrimary;
        ClientSize = new Size(640, 720);
        ForeColor = ThemeHelper.TextPrimary;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Name = "SecurityCenterForm";
        StartPosition = FormStartPosition.CenterParent;
        Text = "Security Center";
        Load += SecurityCenterFormLoad;

        ResumeLayout(false);
    }

    private void BuildIdentityCard()
    {
        var card = CreateCard("Identity Card", 252);
        _avatarBox = new PictureBox { Location = new Point(16, 54), Size = new Size(72, 72), SizeMode = PictureBoxSizeMode.Zoom };
        _displayNameLabel = new Label
        {
            Location = new Point(104, 60),
            Size = new Size(430, 28),
            Font = new Font("Segoe UI", 14F, FontStyle.Bold),
            ForeColor = ThemeHelper.TextPrimary
        };
        var fingerprintTitle = CreateTextLabel("Your Public Key Fingerprint:", 104, 92, 230, ThemeHelper.TextSecondary);
        _fingerprintPanel = new FlowLayoutPanel
        {
            Location = new Point(16, 134),
            Size = new Size(540, 72),
            WrapContents = true,
            BackColor = ThemeHelper.BgCard
        };
        _copyFingerprintButton = CreateButton("Copy Fingerprint", 16, 214, 160, ThemeHelper.BgHover);
        _exportPublicKeyButton = CreateButton("Export Public Key", 184, 214, 160, ThemeHelper.BgHover);
        _copyFingerprintButton.Click += CopyFingerprintButtonClick;
        _exportPublicKeyButton.Click += ExportPublicKeyButtonClick;

        card.Controls.Add(_avatarBox);
        card.Controls.Add(_displayNameLabel);
        card.Controls.Add(fingerprintTitle);
        card.Controls.Add(_fingerprintPanel);
        card.Controls.Add(_copyFingerprintButton);
        card.Controls.Add(_exportPublicKeyButton);
        _contentPanel.Controls.Add(card);
    }

    private void BuildCurrentDeviceCard()
    {
        var card = CreateCard("Current Device", 190);
        _deviceIdLabel = CreateTextLabel("Device ID: Unknown", 16, 54, 520, ThemeHelper.TextPrimary);
        _deviceNameLabel = CreateTextLabel("Device Name: Unknown", 16, 80, 520, ThemeHelper.TextPrimary);
        _createdAtLabel = CreateTextLabel("Created At: Unknown", 16, 106, 520, ThemeHelper.TextSecondary);
        _lastLoginLabel = CreateTextLabel("Last Login: Unknown", 16, 132, 520, ThemeHelper.TextSecondary);
        _jwtStatusLabel = CreateTextLabel("JWT Status: Unknown", 16, 158, 310, ThemeHelper.TextSecondary);
        _refreshTokenButton = CreateButton("Refresh Token", 390, 148, 150, ThemeHelper.BgHover);
        _refreshTokenButton.Click += RefreshTokenButtonClick;

        card.Controls.Add(_deviceIdLabel);
        card.Controls.Add(_deviceNameLabel);
        card.Controls.Add(_createdAtLabel);
        card.Controls.Add(_lastLoginLabel);
        card.Controls.Add(_jwtStatusLabel);
        card.Controls.Add(_refreshTokenButton);
        _contentPanel.Controls.Add(card);
    }

    private void BuildSecurityStatusCard()
    {
        var card = CreateCard("Security Status", 178);
        var statuses = new[]
        {
            "✅ TLS 1.3 Active",
            "✅ RSA 4096-bit Key",
            "✅ AES-256-GCM Encryption",
            "✅ End-to-End Encrypted",
            "✅ Private Key: Local Only (DPAPI Protected)"
        };

        for (var i = 0; i < statuses.Length; i++)
        {
            card.Controls.Add(CreateTextLabel(statuses[i], 16, 54 + i * 24, 520, ThemeHelper.AccentGreen));
        }

        _contentPanel.Controls.Add(card);
    }

    private void BuildDangerZoneCard()
    {
        var card = CreateCard("Danger Zone", 166);
        card.Controls.Add(CreateTextLabel("High impact actions require confirmation.", 16, 54, 520, ThemeHelper.TextSecondary));
        _regenerateKeyButton = CreateButton("Regenerate RSA Key Pair", 16, 86, 190, ThemeHelper.Danger);
        _logoutAllDevicesButton = CreateButton("Logout All Devices", 214, 86, 160, ThemeHelper.Danger);
        _exportKeyBackupButton = CreateButton("Export Key Backup", 382, 86, 160, ThemeHelper.BgHover);
        _regenerateKeyButton.Click += RegenerateKeyButtonClick;
        _logoutAllDevicesButton.Click += LogoutAllDevicesButtonClick;
        _exportKeyBackupButton.Click += ExportKeyBackupButtonClick;

        card.Controls.Add(_regenerateKeyButton);
        card.Controls.Add(_logoutAllDevicesButton);
        card.Controls.Add(_exportKeyBackupButton);
        _contentPanel.Controls.Add(card);
    }

    private Panel CreateCard(string title, int height)
    {
        var card = new Panel
        {
            Size = new Size(580, height),
            Margin = new Padding(0, 0, 0, 14),
            BackColor = ThemeHelper.BgCard
        };
        card.Paint += (_, e) =>
        {
            using var pen = new Pen(ThemeHelper.Border);
            e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
        };
        card.Controls.Add(new Label
        {
            Text = title,
            Location = new Point(16, 16),
            Size = new Size(420, 28),
            Font = new Font("Segoe UI", 13F, FontStyle.Bold),
            ForeColor = ThemeHelper.TextPrimary
        });
        return card;
    }

    private static Label CreateTextLabel(string text, int x, int y, int width, Color color)
    {
        return new Label
        {
            Text = text,
            Location = new Point(x, y),
            Size = new Size(width, 24),
            ForeColor = color,
            AutoEllipsis = true
        };
    }

    private static Button CreateButton(string text, int x, int y, int width, Color color)
    {
        var button = new Button
        {
            Text = text,
            Location = new Point(x, y),
            Size = new Size(width, 32),
            FlatStyle = FlatStyle.Flat,
            BackColor = color,
            ForeColor = Color.White
        };
        button.FlatAppearance.BorderColor = ThemeHelper.Border;
        return button;
    }
}
