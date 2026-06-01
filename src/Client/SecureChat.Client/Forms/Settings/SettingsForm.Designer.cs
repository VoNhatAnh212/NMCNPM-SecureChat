#nullable enable

using SecureChat.Client.Forms.Helpers;

namespace SecureChat.Client.Forms.Settings;

/// <summary>
/// Designer portion of <see cref="SettingsForm"/>.
/// </summary>
public partial class SettingsForm
{
    private Panel _sidebarPanel = null!;
    private Panel _contentPanel = null!;
    private Button _generalNavButton = null!;
    private Button _notificationsNavButton = null!;
    private Button _securityNavButton = null!;
    private Button _aboutNavButton = null!;
    private Panel _generalPage = null!;
    private Panel _notificationsPage = null!;
    private Panel _securityPage = null!;
    private Panel _aboutPage = null!;
    private RadioButton _darkThemeRadio = null!;
    private RadioButton _lightThemeRadio = null!;
    private ComboBox _languageCombo = null!;
    private CheckBox _startWithWindowsCheck = null!;
    private CheckBox _minimizeToTrayCheck = null!;
    private CheckBox _enableNotificationsCheck = null!;
    private CheckBox _messageSoundCheck = null!;
    private ComboBox _soundCombo = null!;
    private CheckBox _groupMessagesCheck = null!;
    private CheckBox _friendRequestsCheck = null!;
    private Label _currentDeviceLabel = null!;
    private Button _manageDevicesButton = null!;
    private Button _securityCenterButton = null!;
    private Button _logoutButton = null!;
    private Button _logoutAllButton = null!;

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        SuspendLayout();

        _sidebarPanel = new Panel
        {
            Dock = DockStyle.Left,
            Width = 140,
            BackColor = ThemeHelper.BgSecondary,
            Padding = new Padding(10, 18, 10, 10)
        };

        _contentPanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = ThemeHelper.BgPrimary,
            Padding = new Padding(22)
        };

        _generalNavButton = CreateNavButton("General");
        _notificationsNavButton = CreateNavButton("Notifications");
        _securityNavButton = CreateNavButton("Security");
        _aboutNavButton = CreateNavButton("About");

        _sidebarPanel.Controls.Add(_aboutNavButton);
        _sidebarPanel.Controls.Add(_securityNavButton);
        _sidebarPanel.Controls.Add(_notificationsNavButton);
        _sidebarPanel.Controls.Add(_generalNavButton);

        _generalPage = CreatePage();
        _notificationsPage = CreatePage();
        _securityPage = CreatePage();
        _aboutPage = CreatePage();

        BuildGeneralPage();
        BuildNotificationsPage();
        BuildSecurityPage();
        BuildAboutPage();

        _contentPanel.Controls.Add(_aboutPage);
        _contentPanel.Controls.Add(_securityPage);
        _contentPanel.Controls.Add(_notificationsPage);
        _contentPanel.Controls.Add(_generalPage);

        Controls.Add(_contentPanel);
        Controls.Add(_sidebarPanel);

        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        BackColor = ThemeHelper.BgPrimary;
        ClientSize = new Size(560, 640);
        ForeColor = ThemeHelper.TextPrimary;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Name = "SettingsForm";
        StartPosition = FormStartPosition.CenterParent;
        Text = "Settings";
        Load += SettingsFormLoad;

        ResumeLayout(false);
    }

    private Button CreateNavButton(string text)
    {
        var button = new Button
        {
            Text = text,
            Dock = DockStyle.Top,
            Height = 42,
            FlatStyle = FlatStyle.Flat,
            BackColor = ThemeHelper.BgSecondary,
            ForeColor = ThemeHelper.TextPrimary,
            TextAlign = ContentAlignment.MiddleLeft
        };
        button.FlatAppearance.BorderSize = 0;
        button.Click += NavButtonClick;
        return button;
    }

    private static Panel CreatePage()
    {
        return new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            Visible = false
        };
    }

    private void BuildGeneralPage()
    {
        var title = CreateTitle("General");
        _darkThemeRadio = CreateRadio("Dark");
        _lightThemeRadio = CreateRadio("Light");
        _languageCombo = CreateCombo("EN", "VI");
        _startWithWindowsCheck = CreateCheck("Start with Windows");
        _minimizeToTrayCheck = CreateCheck("Minimize to tray");

        _generalPage.Controls.Add(_minimizeToTrayCheck);
        _generalPage.Controls.Add(_startWithWindowsCheck);
        _generalPage.Controls.Add(CreateLabeledControl("Language", _languageCombo));
        _generalPage.Controls.Add(_lightThemeRadio);
        _generalPage.Controls.Add(_darkThemeRadio);
        _generalPage.Controls.Add(CreateSectionLabel("Theme"));
        _generalPage.Controls.Add(title);
    }

    private void BuildNotificationsPage()
    {
        _enableNotificationsCheck = CreateCheck("Enable notifications");
        _messageSoundCheck = CreateCheck("Message sound");
        _soundCombo = CreateCombo("Default", "Soft", "None");
        _groupMessagesCheck = CreateCheck("Group messages");
        _friendRequestsCheck = CreateCheck("Friend requests");

        _notificationsPage.Controls.Add(_friendRequestsCheck);
        _notificationsPage.Controls.Add(_groupMessagesCheck);
        _notificationsPage.Controls.Add(CreateLabeledControl("Sound", _soundCombo));
        _notificationsPage.Controls.Add(_messageSoundCheck);
        _notificationsPage.Controls.Add(_enableNotificationsCheck);
        _notificationsPage.Controls.Add(CreateTitle("Notifications"));
    }

    private void BuildSecurityPage()
    {
        _currentDeviceLabel = CreateSectionLabel("Current Device: Unknown");
        _manageDevicesButton = CreateActionButton("Manage Devices");
        _securityCenterButton = CreateActionButton("Security Center");
        _logoutButton = CreateActionButton("Logout");
        _logoutAllButton = CreateActionButton("Logout All Devices");
        _logoutButton.BackColor = ThemeHelper.Danger;
        _logoutAllButton.BackColor = ThemeHelper.Danger;

        _manageDevicesButton.Click += ManageDevicesButtonClick;
        _securityCenterButton.Click += SecurityCenterButtonClick;
        _logoutButton.Click += LogoutButtonClick;
        _logoutAllButton.Click += LogoutAllButtonClick;

        _securityPage.Controls.Add(_logoutAllButton);
        _securityPage.Controls.Add(_logoutButton);
        _securityPage.Controls.Add(_securityCenterButton);
        _securityPage.Controls.Add(_manageDevicesButton);
        _securityPage.Controls.Add(_currentDeviceLabel);
        _securityPage.Controls.Add(CreateTitle("Security"));
    }

    private void BuildAboutPage()
    {
        _aboutPage.Controls.Add(CreateSectionLabel(".NET " + Environment.Version));
        _aboutPage.Controls.Add(CreateSectionLabel("SecureChat is open source"));
        _aboutPage.Controls.Add(CreateSectionLabel("TLS | RSA | AES-GCM | E2EE"));
        _aboutPage.Controls.Add(CreateSectionLabel("App version 1.0.0"));
        _aboutPage.Controls.Add(CreateTitle("About"));
    }

    private Label CreateTitle(string text)
    {
        return new Label
        {
            Text = text,
            Dock = DockStyle.Top,
            Height = 46,
            Font = new Font("Segoe UI", 18F, FontStyle.Bold),
            ForeColor = ThemeHelper.TextPrimary
        };
    }

    private Label CreateSectionLabel(string text)
    {
        return new Label
        {
            Text = text,
            Dock = DockStyle.Top,
            Height = 34,
            Padding = new Padding(0, 8, 0, 0),
            ForeColor = ThemeHelper.TextSecondary
        };
    }

    private RadioButton CreateRadio(string text)
    {
        var radio = new RadioButton
        {
            Text = text,
            Dock = DockStyle.Top,
            Height = 32,
            ForeColor = ThemeHelper.TextPrimary
        };
        radio.CheckedChanged += SettingChanged;
        return radio;
    }

    private CheckBox CreateCheck(string text)
    {
        var check = new CheckBox
        {
            Text = text,
            Dock = DockStyle.Top,
            Height = 34,
            ForeColor = ThemeHelper.TextPrimary
        };
        check.CheckedChanged += SettingChanged;
        return check;
    }

    private ComboBox CreateCombo(params string[] values)
    {
        var combo = new ComboBox
        {
            Dock = DockStyle.Top,
            DropDownStyle = ComboBoxStyle.DropDownList,
            BackColor = ThemeHelper.BgCard,
            ForeColor = ThemeHelper.TextPrimary
        };
        combo.Items.AddRange(values);
        combo.SelectedIndexChanged += SettingChanged;
        return combo;
    }

    private Control CreateLabeledControl(string label, Control control)
    {
        var panel = new Panel { Dock = DockStyle.Top, Height = 62 };
        panel.Controls.Add(control);
        panel.Controls.Add(CreateSectionLabel(label));
        return panel;
    }

    private Button CreateActionButton(string text)
    {
        var button = new Button
        {
            Text = text,
            Dock = DockStyle.Top,
            Height = 42,
            Margin = new Padding(0, 4, 0, 4),
            FlatStyle = FlatStyle.Flat,
            BackColor = ThemeHelper.BgCard,
            ForeColor = ThemeHelper.TextPrimary
        };
        button.FlatAppearance.BorderColor = ThemeHelper.Border;
        return button;
    }
}
