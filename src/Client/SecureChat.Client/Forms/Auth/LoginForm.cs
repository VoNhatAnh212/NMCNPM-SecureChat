#nullable enable

using Microsoft.Extensions.DependencyInjection;
using SecureChat.Client.Controllers;
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
        _loginButton.Text = isLoading ? LoggingInText : LoginButtonText;
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
}
