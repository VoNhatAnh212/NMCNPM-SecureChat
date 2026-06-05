#nullable enable

using SecureChat.Client.Forms.Controls;
using SecureChat.Client.Forms.Helpers;

namespace SecureChat.Client.Forms.Auth;

/// <summary>
/// Designer portion of <see cref="LoginForm"/>.
/// </summary>
public partial class LoginForm
{
    private PictureBox _logoPictureBox = null!;
    private Label _titleLabel = null!;
    private Label _subtitleLabel = null!;
    private Label _savedAccountsLabel = null!;
    private AccountListPanel _accountsPanel = null!;
    private Label _emptyLabel = null!;
    private RoundedButton _loginButton = null!;
    private Label _orLabel = null!;
    private RoundedButton _addDeviceButton = null!;
    private Button _removeAccountButton = null!;
    private RoundedButton _passwordLoginButton = null!;
    private Label _errorLabel = null!;

    /// <summary>
    /// Releases resources used by the form.
    /// </summary>
    /// <param name="disposing">A value indicating whether managed resources should be disposed.</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _logoPictureBox?.Dispose();
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
            Text = "Welcome back",
            Font = new Font("Segoe UI", 24F, FontStyle.Bold, GraphicsUnit.Point),
            ForeColor = ThemeHelper.TextPrimary,
            Location = new Point(32, 114),
            Size = new Size(376, 42),
            TextAlign = ContentAlignment.MiddleCenter
        };

        _subtitleLabel = new Label
        {
            AutoSize = false,
            Text = "Sign in to your device",
            Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point),
            ForeColor = ThemeHelper.TextMuted,
            Location = new Point(32, 156),
            Size = new Size(376, 24),
            TextAlign = ContentAlignment.MiddleCenter
        };

        _savedAccountsLabel = new Label
        {
            AutoSize = false,
            Text = "Saved Accounts",
            Font = new Font("Segoe UI", 8.5F, FontStyle.Regular, GraphicsUnit.Point),
            ForeColor = ThemeHelper.TextMuted,
            Location = new Point(34, 218),
            Size = new Size(200, 20)
        };

        _accountsPanel = new AccountListPanel
        {
            Location = new Point(32, 242),
            Size = new Size(376, 210),
            BorderStyle = BorderStyle.None
        };
        _accountsPanel.SelectedAccountChanged += AccountSelectionChanged;
        _accountsPanel.AccountActivated += AccountActivated;

        _emptyLabel = new Label
        {
            AutoSize = false,
            Text = "No saved accounts. Create an account to start using SecureChat.",
            Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point),
            ForeColor = ThemeHelper.TextMuted,
            Location = new Point(56, 310),
            Size = new Size(328, 56),
            TextAlign = ContentAlignment.MiddleCenter,
            Visible = false
        };

        _loginButton = new RoundedButton
        {
            Text = "Login with selected account",
            Location = new Point(32, 470),
            Size = new Size(376, 46),
            Enabled = false
        };
        _loginButton.Click += LoginButtonClick;

        _orLabel = new Label
        {
            AutoSize = false,
            Text = "or",
            Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point),
            ForeColor = ThemeHelper.TextMuted,
            Location = new Point(32, 524),
            Size = new Size(376, 18),
            TextAlign = ContentAlignment.MiddleCenter
        };

        _passwordLoginButton = new RoundedButton
        {
            Text = "Login with other account",
            BackColor = ThemeHelper.AccentBlue,
            ForeColor = Color.White,
            Location = new Point(32, 552),
            Size = new Size(184, 42)
        };
        _passwordLoginButton.Click += PasswordLoginButtonClick;

        _addDeviceButton = new RoundedButton
        {
            Text = "Create account",
            BackColor = ThemeHelper.BgCard,
            HoverBackColor = ThemeHelper.BgHover,
            ForeColor = ThemeHelper.TextPrimary,
            Location = new Point(224, 552),
            Size = new Size(184, 42)
        };
        _addDeviceButton.Click += AddDeviceButtonClick;

        _removeAccountButton = new Button
        {
            Text = "Remove account",
            FlatStyle = FlatStyle.Flat,
            BackColor = ThemeHelper.BgPrimary,
            ForeColor = ThemeHelper.TextMuted,
            Font = new Font("Segoe UI", 8.5F, FontStyle.Regular, GraphicsUnit.Point),
            Location = new Point(152, 606),
            Size = new Size(136, 26),
            Cursor = Cursors.Hand
        };
        _removeAccountButton.FlatAppearance.BorderSize = 0;
        _removeAccountButton.Click += RemoveAccountButtonClick;

        _errorLabel = new Label
        {
            AutoSize = false,
            ForeColor = ThemeHelper.Danger,
            Location = new Point(32, 188),
            Size = new Size(376, 20),
            TextAlign = ContentAlignment.MiddleCenter,
            Visible = false
        };

        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        BackColor = ThemeHelper.BgPrimary;
        ClientSize = new Size(440, 650);
        Controls.Add(_errorLabel);
        Controls.Add(_passwordLoginButton);
        Controls.Add(_removeAccountButton);
        Controls.Add(_addDeviceButton);
        Controls.Add(_orLabel);
        Controls.Add(_loginButton);
        Controls.Add(_emptyLabel);
        Controls.Add(_accountsPanel);
        Controls.Add(_savedAccountsLabel);
        Controls.Add(_subtitleLabel);
        Controls.Add(_titleLabel);
        Controls.Add(_logoPictureBox);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        Name = "LoginForm";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "SecureChat Login";

        ResumeLayout(false);
    }

    private static void LogoPictureBoxPaint(object? sender, PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        using var fill = new SolidBrush(ThemeHelper.AccentBlue);
        e.Graphics.FillEllipse(fill, new Rectangle(0, 0, 79, 79));
        using var lockFill = new SolidBrush(ThemeHelper.BgPrimary);
        e.Graphics.FillRectangle(lockFill, new Rectangle(25, 38, 30, 22));
        using var pen = new Pen(ThemeHelper.BgPrimary, 5F);
        e.Graphics.DrawArc(pen, new Rectangle(24, 24, 32, 28), 200, 140);
    }
}
