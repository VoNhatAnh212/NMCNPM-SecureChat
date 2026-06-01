#nullable enable

using Microsoft.Extensions.DependencyInjection;
using SecureChat.Client.Forms.Auth;
using SecureChat.Client.Forms.Helpers;
using SecureChat.Client.Forms.Main;
using SecureChat.Client.Services.Crypto;
using SecureChat.Client.Services.SignalR;

namespace SecureChat.Client.Forms.Splash;

/// <summary>
/// Displays startup progress and routes the user to the next screen.
/// </summary>
public partial class SplashForm : Form
{
    private const string CheckingKeysStatus = "Checking saved session...";
    private const string LoadingAccountStatus = "Loading account...";
    private const string VerifyingSessionStatus = "Verifying session...";
    private const string ConnectingStatus = "Connecting...";
    private const int SmoothDelayMilliseconds = 800;

    private readonly IServiceProvider _serviceProvider;
    private readonly KeyStorageService _keyStorageService;
    private readonly ISignalRService _signalRService;

    /// <summary>
    /// Initializes a new instance of the <see cref="SplashForm"/> class.
    /// </summary>
    /// <param name="serviceProvider">The application service provider.</param>
    public SplashForm(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _keyStorageService = serviceProvider.GetRequiredService<KeyStorageService>();
        _signalRService = serviceProvider.GetRequiredService<ISignalRService>();
        InitializeComponent();
        ThemeHelper.ApplyTheme(this);
    }

    /// <inheritdoc />
    protected override async void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        await RunStartupAsync();
    }

    private async Task RunStartupAsync()
    {
        try
        {
            SetStatus(CheckingKeysStatus, 20);
            await Task.Delay(SmoothDelayMilliseconds / 4);

            SetStatus(LoadingAccountStatus, 45);
            _ = await _keyStorageService.LoadAccountsAsync();

            SetStatus(VerifyingSessionStatus, 70);
            var tokens = await _keyStorageService.LoadTokensAsync();
            await Task.Delay(SmoothDelayMilliseconds);

            if (!string.IsNullOrWhiteSpace(tokens.access) && tokens.expiry > DateTime.UtcNow)
            {
                SetStatus(ConnectingStatus, 92);
                await _signalRService.ConnectAsync(tokens.access);
                SetStatus(ConnectingStatus, 100);
                OpenNext(new MainForm(_serviceProvider));
                return;
            }

            OpenNext(new LoginForm(_serviceProvider));
        }
        catch
        {
            OpenNext(new LoginForm(_serviceProvider));
        }
    }

    private void SetStatus(string status, int progress)
    {
        if (InvokeRequired)
        {
            BeginInvoke(() => SetStatus(status, progress));
            return;
        }

        _statusLabel.Text = status;
        _progressBar.Value = progress;
    }

    private void OpenNext(Form form)
    {
        if (InvokeRequired)
        {
            BeginInvoke(() => OpenNext(form));
            return;
        }

        Hide();
        form.FormClosed += (_, _) => Close();
        form.Show();
    }
}
