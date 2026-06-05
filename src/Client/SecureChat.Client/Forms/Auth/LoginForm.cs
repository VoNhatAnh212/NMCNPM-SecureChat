#nullable enable

using Microsoft.Extensions.DependencyInjection;
using SecureChat.Client.Controllers;
using SecureChat.Client.Forms.Controls;
using SecureChat.Client.Forms.Helpers;
using SecureChat.Client.Forms.Main;
using SecureChat.Client.Models;
using SecureChat.Client.Services.Crypto;
using AuthFlowController = SecureChat.Client.Controllers.AuthController;

namespace SecureChat.Client.Forms.Auth;

/// <summary>
/// Allows a user to sign in with a saved local account.
/// </summary>
public partial class LoginForm : Form
{
    private const string LoadingAccountsError = "Unable to load saved accounts.";
    private const string SelectAccountError = "Select an account before signing in.";
    private const string LoginFailedError = "Sign in failed. Check this device and try again.";
    private const string LoggingInText = "Signing in...";
    private const string LoginButtonText = "Login with selected account";
    private const string NoSavedAccountsText = "No saved accounts. Create an account to start using SecureChat.";
    private const string PasswordLoginButtonText = "Login with other account";
    private const string InvalidPasswordLoginError = "Enter email/username and password.";

    private readonly IServiceProvider _serviceProvider;
    private readonly AuthFlowController _authController;
    private readonly KeyStorageService _keyStorageService;
    private LocalUser? _selectedAccount;

    /// <summary>
    /// Initializes a new instance of the <see cref="LoginForm"/> class.
    /// </summary>
    /// <param name="serviceProvider">The application service provider.</param>
    public LoginForm(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _authController = serviceProvider.GetRequiredService<AuthFlowController>();
        _keyStorageService = serviceProvider.GetRequiredService<KeyStorageService>();
        InitializeComponent();
        ThemeHelper.ApplyTheme(this);
    }

    /// <inheritdoc />
    protected override async void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        await LoadAccountsAsync();
    }

    private async Task LoadAccountsAsync()
    {
        try
        {
            var accounts = await _keyStorageService.LoadAccountsAsync();
            _accountsPanel.SetAccounts(accounts);
            _emptyLabel.Visible = accounts.Count == 0;
            _emptyLabel.Text = accounts.Count == 0 ? NoSavedAccountsText : string.Empty;
            _removeAccountButton.Visible = accounts.Count > 0;
        }
        catch
        {
            ShowError(LoadingAccountsError);
        }
    }

    private async void LoginButtonClick(object? sender, EventArgs e)
    {
        await LoginSelectedAccountAsync();
    }

    private async void AccountActivated(object? sender, LocalUser e)
    {
        _selectedAccount = e;
        await LoginSelectedAccountAsync();
    }

    private async Task LoginSelectedAccountAsync()
    {
        if (_selectedAccount is null)
        {
            ShowError(SelectAccountError);
            return;
        }

        SetLoading(true);
        try
        {
            var success = await _authController.LoginAsync(_selectedAccount.DeviceId);
            if (!success)
            {
                ShowError(LoginFailedError);
                return;
            }

            OpenMainForm();
        }
        catch
        {
            ShowError(LoginFailedError);
        }
        finally
        {
            SetLoading(false);
        }
    }

    private void AccountSelectionChanged(object? sender, LocalUser? account)
    {
        _selectedAccount = account;
        _loginButton.Enabled = account is not null;
        _errorLabel.Visible = false;
    }

    private void AddDeviceButtonClick(object? sender, EventArgs e)
    {
        Hide();
        var registerForm = new RegisterForm(_serviceProvider);
        registerForm.FormClosed += (_, _) => Close();
        registerForm.Show();
    }

    private async void PasswordLoginButtonClick(object? sender, EventArgs e)
    {
        await LoginWithPasswordAsync();
    }

    private async Task LoginWithPasswordAsync()
    {
        var credentials = PromptForOtherAccount();
        if (credentials is null)
        {
            return;
        }

        var (username, password) = credentials.Value;
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            ShowError(InvalidPasswordLoginError);
            return;
        }

        SetLoading(true);
        try
        {
            var status = await _authController.StartPasswordLoginAsync(username, password);
            if (status == AuthFlowController.PasswordLoginStartStatus.LoggedIn)
            {
                OpenMainForm();
                return;
            }

            var otp = PromptForOtp(username);
            if (string.IsNullOrWhiteSpace(otp))
            {
                return;
            }

            var deviceName = PromptForDeviceName();
            var success = await _authController.CompletePasswordDeviceLoginAsync(username, password, otp, deviceName);
            if (!success)
            {
                ShowError(LoginFailedError);
                return;
            }

            OpenMainForm();
        }
        catch (Exception ex)
        {
            ShowError(string.IsNullOrWhiteSpace(ex.Message) ? LoginFailedError : ex.Message);
        }
        finally
        {
            SetLoading(false);
        }
    }

    private async void RemoveAccountButtonClick(object? sender, EventArgs e)
    {
        if (_selectedAccount is null)
        {
            return;
        }

        await _keyStorageService.DeletePrivateKeyAsync(_selectedAccount.DeviceId);
        await LoadAccountsAsync();
    }

    private void SetLoading(bool isLoading)
    {
        if (InvokeRequired)
        {
            BeginInvoke(() => SetLoading(isLoading));
            return;
        }

        _loginButton.Enabled = !isLoading && _selectedAccount is not null;
        _addDeviceButton.Enabled = !isLoading;
        _removeAccountButton.Enabled = !isLoading;
        _passwordLoginButton.Enabled = !isLoading;
        _loginButton.Text = isLoading ? LoggingInText : LoginButtonText;
        _passwordLoginButton.Text = isLoading ? LoggingInText : PasswordLoginButtonText;
    }

    private void ShowError(string message)
    {
        if (InvokeRequired)
        {
            BeginInvoke(() => ShowError(message));
            return;
        }

        _errorLabel.Text = message;
        _errorLabel.Visible = true;
    }

    private void OpenMainForm()
    {
        if (InvokeRequired)
        {
            BeginInvoke(OpenMainForm);
            return;
        }

        Hide();
        var mainForm = new MainForm(_serviceProvider);
        mainForm.FormClosed += (_, _) => Close();
        mainForm.Show();
    }

    private (string Username, string Password)? PromptForOtherAccount()
    {
        using var form = new Form
        {
            Text = "Login with other account",
            StartPosition = FormStartPosition.CenterParent,
            Size = new Size(400, 260),
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false,
            BackColor = ThemeHelper.BgPrimary,
            ForeColor = ThemeHelper.TextPrimary
        };

        var title = new Label
        {
            Text = "Login with other account",
            Location = new Point(20, 18),
            Size = new Size(340, 26),
            Font = new Font("Segoe UI", 12F, FontStyle.Bold),
            ForeColor = ThemeHelper.TextPrimary
        };
        var subtitle = new Label
        {
            Text = "Use email/username and password. A new device requires OTP.",
            Location = new Point(20, 46),
            Size = new Size(340, 34),
            ForeColor = ThemeHelper.TextMuted
        };
        var usernameBox = new RoundedTextBox
        {
            PlaceholderText = "Email / username",
            Location = new Point(20, 88),
            Size = new Size(344, 40)
        };
        var passwordBox = new RoundedTextBox
        {
            PlaceholderText = "Password",
            Location = new Point(20, 138),
            Size = new Size(344, 40),
            UseSystemPasswordChar = true
        };
        var loginButton = new RoundedButton
        {
            Text = "Continue",
            Location = new Point(224, 190),
            Size = new Size(140, 34),
            DialogResult = DialogResult.OK
        };
        var cancelButton = new Button
        {
            Text = "Cancel",
            Location = new Point(120, 190),
            Size = new Size(92, 34),
            DialogResult = DialogResult.Cancel,
            FlatStyle = FlatStyle.Flat,
            BackColor = ThemeHelper.BgCard,
            ForeColor = ThemeHelper.TextPrimary
        };
        cancelButton.FlatAppearance.BorderColor = ThemeHelper.Border;

        form.AcceptButton = loginButton;
        form.CancelButton = cancelButton;
        form.Controls.Add(title);
        form.Controls.Add(subtitle);
        form.Controls.Add(usernameBox);
        form.Controls.Add(passwordBox);
        form.Controls.Add(loginButton);
        form.Controls.Add(cancelButton);

        return form.ShowDialog(this) == DialogResult.OK
            ? (usernameBox.Text.Trim(), passwordBox.Text)
            : null;
    }

    private string? PromptForOtp(string username)
    {
        using var form = CreatePromptForm("Verify new device", $"Enter the 6 digit OTP sent to {username}.");
        var input = CreatePromptTextBox("OTP code");
        input.MaxLength = 6;
        form.Controls.Add(input);
        return form.ShowDialog(this) == DialogResult.OK ? input.Text.Trim() : null;
    }

    private string PromptForDeviceName()
    {
        using var form = CreatePromptForm("Trust this device", "Name this device.");
        var input = CreatePromptTextBox("Device name");
        input.Text = Environment.MachineName;
        form.Controls.Add(input);
        return form.ShowDialog(this) == DialogResult.OK && !string.IsNullOrWhiteSpace(input.Text)
            ? input.Text.Trim()
            : Environment.MachineName;
    }

    private static Form CreatePromptForm(string title, string message)
    {
        var form = new Form
        {
            Text = title,
            StartPosition = FormStartPosition.CenterParent,
            Size = new Size(360, 190),
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false,
            BackColor = ThemeHelper.BgPrimary,
            ForeColor = ThemeHelper.TextPrimary
        };
        var label = new Label
        {
            Text = message,
            Location = new Point(16, 16),
            Size = new Size(312, 36),
            ForeColor = ThemeHelper.TextPrimary
        };
        var okButton = new RoundedButton
        {
            Text = "Continue",
            Location = new Point(196, 108),
            Size = new Size(132, 34),
            DialogResult = DialogResult.OK
        };
        var cancelButton = new Button
        {
            Text = "Cancel",
            Location = new Point(96, 108),
            Size = new Size(88, 34),
            DialogResult = DialogResult.Cancel,
            FlatStyle = FlatStyle.Flat,
            BackColor = ThemeHelper.BgCard,
            ForeColor = ThemeHelper.TextPrimary
        };
        form.AcceptButton = okButton;
        form.CancelButton = cancelButton;
        form.Controls.Add(label);
        form.Controls.Add(okButton);
        form.Controls.Add(cancelButton);
        return form;
    }

    private static RoundedTextBox CreatePromptTextBox(string placeholder)
    {
        return new RoundedTextBox
        {
            PlaceholderText = placeholder,
            Location = new Point(16, 62),
            Size = new Size(312, 40)
        };
    }
}
