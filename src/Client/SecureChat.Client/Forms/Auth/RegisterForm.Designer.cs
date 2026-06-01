#nullable enable

using SecureChat.Client.Forms.Controls;
using SecureChat.Client.Forms.Helpers;

namespace SecureChat.Client.Forms.Auth;

/// <summary>
/// Designer portion of <see cref="RegisterForm"/>.
/// </summary>
public partial class RegisterForm
{
    private PictureBox _logoPictureBox = null!;
    private Label _titleLabel = null!;
    private Label _stepLabel = null!;
    private Panel _stepOnePanel = null!;
    private Panel _stepTwoPanel = null!;
    private Panel _stepThreePanel = null!;
    private RoundedTextBox _emailTextBox = null!;
    private RoundedTextBox _displayNameTextBox = null!;
    private RoundedTextBox _passwordTextBox = null!;
    private RoundedTextBox _otpTextBox = null!;
    private RoundedTextBox _deviceNameTextBox = null!;
    private RoundedButton _sendOtpButton = null!;
    private RoundedButton _verifyOtpButton = null!;
    private Button _backToLoginButton = null!;
    private LinkLabel _resendOtpLink = null!;
    private Label _timerLabel = null!;
    private Label _errorLabel = null!;
    private SecureProgressBar _setupProgressBar = null!;
    private Label _setupStatusLabel = null!;

    /// <summary>
    /// Releases resources used by the form.
    /// </summary>
    /// <param name="disposing">A value indicating whether managed resources should be disposed.</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _logoPictureBox?.Dispose();
            _otpTimer?.Dispose();
            _fadeTimer?.Dispose();
        }

        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        SuspendLayout();

        _logoPictureBox = new PictureBox
        {
            Location = new Point(180, 24),
            Size = new Size(80, 80),
            BackColor = Color.Transparent
        };
        _logoPictureBox.Paint += LogoPictureBoxPaint;

        _titleLabel = new Label
        {
            AutoSize = false,
            Text = "Create Account",
            Font = new Font("Segoe UI", 24F, FontStyle.Bold, GraphicsUnit.Point),
            ForeColor = ThemeHelper.TextPrimary,
            Location = new Point(32, 116),
            Size = new Size(376, 42),
            TextAlign = ContentAlignment.MiddleCenter
        };

        _stepLabel = new Label
        {
            AutoSize = false,
            Text = "Step 1 of 3: Enter your details",
            ForeColor = ThemeHelper.TextMuted,
            Location = new Point(32, 162),
            Size = new Size(376, 24),
            TextAlign = ContentAlignment.MiddleCenter
        };

        _errorLabel = new Label
        {
            AutoSize = false,
            ForeColor = ThemeHelper.Danger,
            Location = new Point(32, 192),
            Size = new Size(376, 24),
            TextAlign = ContentAlignment.MiddleCenter,
            Visible = false
        };

        BuildStepOnePanel();
        BuildStepTwoPanel();
        BuildStepThreePanel();

        _backToLoginButton = new Button
        {
            Text = "Back to Login",
            Location = new Point(32, 548),
            Size = new Size(376, 36),
            FlatStyle = FlatStyle.Flat,
            BackColor = ThemeHelper.BgCard,
            ForeColor = ThemeHelper.TextPrimary,
            Cursor = Cursors.Hand
        };
        _backToLoginButton.FlatAppearance.BorderColor = ThemeHelper.Border;
        _backToLoginButton.Click += BackToLoginButtonClick;

        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        BackColor = ThemeHelper.BgPrimary;
        ClientSize = new Size(440, 620);
        Controls.Add(_stepThreePanel);
        Controls.Add(_stepTwoPanel);
        Controls.Add(_stepOnePanel);
        Controls.Add(_errorLabel);
        Controls.Add(_stepLabel);
        Controls.Add(_titleLabel);
        Controls.Add(_logoPictureBox);
        Controls.Add(_backToLoginButton);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        Name = "RegisterForm";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "Create SecureChat Account";

        ResumeLayout(false);
    }

    private void BuildStepOnePanel()
    {
        _stepOnePanel = new Panel
        {
            Location = new Point(32, 230),
            Size = new Size(376, 290),
            BackColor = ThemeHelper.BgPrimary
        };

        _emailTextBox = new RoundedTextBox
        {
            PlaceholderText = "your@email.com",
            Location = new Point(0, 0),
            Size = new Size(376, 44)
        };

        _displayNameTextBox = new RoundedTextBox
        {
            PlaceholderText = "Your Name",
            Location = new Point(0, 62),
            Size = new Size(376, 44)
        };

        _passwordTextBox = new RoundedTextBox
        {
            PlaceholderText = "Password (8+ characters)",
            Location = new Point(0, 124),
            Size = new Size(376, 44),
            UseSystemPasswordChar = true
        };

        _sendOtpButton = new RoundedButton
        {
            Text = "Send OTP",
            Location = new Point(0, 196),
            Size = new Size(376, 46)
        };
        _sendOtpButton.Click += SendOtpButtonClick;

        _stepOnePanel.Controls.Add(_sendOtpButton);
        _stepOnePanel.Controls.Add(_passwordTextBox);
        _stepOnePanel.Controls.Add(_displayNameTextBox);
        _stepOnePanel.Controls.Add(_emailTextBox);
    }

    private void BuildStepTwoPanel()
    {
        _stepTwoPanel = new Panel
        {
            Location = new Point(32, 230),
            Size = new Size(376, 300),
            BackColor = ThemeHelper.BgPrimary
        };

        var checkEmailLabel = new Label
        {
            AutoSize = false,
            Text = "Check your email for the code",
            ForeColor = ThemeHelper.TextSecondary,
            Location = new Point(0, 0),
            Size = new Size(376, 28),
            TextAlign = ContentAlignment.MiddleCenter
        };

        _otpTextBox = new RoundedTextBox
        {
            PlaceholderText = "000000",
            Location = new Point(66, 46),
            Size = new Size(244, 54),
            MaxLength = 6,
            TextAlign = HorizontalAlignment.Center
        };
        _otpTextBox.InnerTextBox.Font = new Font("Consolas", 18F, FontStyle.Bold, GraphicsUnit.Point);

        _timerLabel = new Label
        {
            AutoSize = false,
            Text = "10:00",
            ForeColor = ThemeHelper.TextMuted,
            Location = new Point(0, 112),
            Size = new Size(376, 24),
            TextAlign = ContentAlignment.MiddleCenter
        };

        _verifyOtpButton = new RoundedButton
        {
            Text = "Verify OTP",
            Location = new Point(0, 156),
            Size = new Size(376, 46)
        };
        _verifyOtpButton.Click += VerifyOtpButtonClick;

        _resendOtpLink = new LinkLabel
        {
            Text = "Resend OTP",
            LinkColor = ThemeHelper.AccentBlue,
            ActiveLinkColor = ThemeHelper.AccentBlue,
            VisitedLinkColor = ThemeHelper.AccentBlue,
            BackColor = ThemeHelper.BgPrimary,
            Location = new Point(130, 220),
            Size = new Size(116, 24),
            TextAlign = ContentAlignment.MiddleCenter
        };
        _resendOtpLink.LinkClicked += ResendOtpLinkClicked;

        _stepTwoPanel.Controls.Add(_resendOtpLink);
        _stepTwoPanel.Controls.Add(_verifyOtpButton);
        _stepTwoPanel.Controls.Add(_timerLabel);
        _stepTwoPanel.Controls.Add(_otpTextBox);
        _stepTwoPanel.Controls.Add(checkEmailLabel);
    }

    private void BuildStepThreePanel()
    {
        _stepThreePanel = new Panel
        {
            Location = new Point(32, 230),
            Size = new Size(376, 300),
            BackColor = ThemeHelper.BgPrimary
        };

        var setupLabel = new Label
        {
            AutoSize = false,
            Text = "Setting up your device...",
            ForeColor = ThemeHelper.TextSecondary,
            Location = new Point(0, 0),
            Size = new Size(376, 28),
            TextAlign = ContentAlignment.MiddleCenter
        };

        _deviceNameTextBox = new RoundedTextBox
        {
            PlaceholderText = Environment.MachineName,
            Location = new Point(0, 54),
            Size = new Size(376, 44)
        };

        _setupProgressBar = new SecureProgressBar
        {
            Location = new Point(0, 136),
            Size = new Size(376, 8),
            Value = 0
        };

        _setupStatusLabel = new Label
        {
            AutoSize = false,
            Text = "Waiting...",
            ForeColor = ThemeHelper.TextMuted,
            Location = new Point(0, 160),
            Size = new Size(376, 28),
            TextAlign = ContentAlignment.MiddleCenter
        };

        _stepThreePanel.Controls.Add(_setupStatusLabel);
        _stepThreePanel.Controls.Add(_setupProgressBar);
        _stepThreePanel.Controls.Add(_deviceNameTextBox);
        _stepThreePanel.Controls.Add(setupLabel);
    }

    private static void LogoPictureBoxPaint(object? sender, PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        using var fill = new SolidBrush(ThemeHelper.AccentGreen);
        e.Graphics.FillEllipse(fill, new Rectangle(0, 0, 79, 79));
        using var pen = new Pen(ThemeHelper.BgPrimary, 5F);
        e.Graphics.DrawLine(pen, 24, 42, 36, 54);
        e.Graphics.DrawLine(pen, 36, 54, 58, 28);
    }
}
