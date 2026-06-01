#nullable enable

using Microsoft.Extensions.DependencyInjection;
using SecureChat.Client.Controllers;
using SecureChat.Client.Forms.Helpers;
using SecureChat.Client.Forms.Main;
using AuthFlowController = SecureChat.Client.Controllers.AuthController;

namespace SecureChat.Client.Forms.Auth;

/// <summary>
/// Guides the user through account registration and device setup.
/// </summary>
public partial class RegisterForm : Form
{
    private const string StepOneText = "Step 1 of 3: Enter your details";
    private const string StepTwoText = "Step 2 of 3: Verify your email";
    private const string StepThreeText = "Step 3 of 3: Set up this device";
    private const string InvalidDetailsError = "Enter a valid email, display name, and password of at least 8 characters.";
    private const string SendOtpFailedError = "Could not send OTP. Try again.";
    private const string InvalidOtpError = "Enter the 6 digit code from your email.";
    private const string VerifyOtpFailedError = "OTP verification failed.";
    private const string GeneratingKeysStatus = "Generating RSA 4096-bit key pair...";
    private const string RegisteringDeviceStatus = "Registering device...";
    private const string LoggingInStatus = "Logging you in...";
    private const int OtpLifetimeSeconds = 600;

    private readonly IServiceProvider _serviceProvider;
    private readonly AuthFlowController _authController;
    private readonly System.Windows.Forms.Timer _otpTimer;
    private readonly System.Windows.Forms.Timer _fadeTimer;
    private int _remainingOtpSeconds;
    private int _currentStep = 1;
    private int _fadeValue = 100;

    /// <summary>
    /// Initializes a new instance of the <see cref="RegisterForm"/> class.
    /// </summary>
    /// <param name="serviceProvider">The application service provider.</param>
    public RegisterForm(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _authController = serviceProvider.GetRequiredService<AuthFlowController>();
        _otpTimer = new System.Windows.Forms.Timer { Interval = 1000 };
        _otpTimer.Tick += OtpTimerTick;
        _fadeTimer = new System.Windows.Forms.Timer { Interval = 16 };
        _fadeTimer.Tick += FadeTimerTick;
        InitializeComponent();
        ThemeHelper.ApplyTheme(this);
        _deviceNameTextBox.Text = Environment.MachineName;
        ShowStep(1);
    }

    private async void SendOtpButtonClick(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_emailTextBox.Text)
            || string.IsNullOrWhiteSpace(_displayNameTextBox.Text)
            || string.IsNullOrWhiteSpace(_passwordTextBox.Text)
            || _passwordTextBox.Text.Length < 8)
        {
            ShowError(InvalidDetailsError);
            return;
        }

        SetStepOneLoading(true);
        try
        {
            var sent = await _authController.RegisterAsync(
                _emailTextBox.Text.Trim(),
                _displayNameTextBox.Text.Trim(),
                _passwordTextBox.Text);
            if (!sent)
            {
                ShowError(SendOtpFailedError);
                return;
            }

            StartOtpTimer();
            ShowStep(2);
        }
        catch (Exception ex)
        {
            ShowError(ex.Message.Contains("Email is already registered", StringComparison.OrdinalIgnoreCase)
                ? "This email already has an account. Please log in instead."
                : SendOtpFailedError);
        }
        finally
        {
            SetStepOneLoading(false);
        }
    }

    private async void VerifyOtpButtonClick(object? sender, EventArgs e)
    {
        if (_otpTextBox.Text.Trim().Length != 6)
        {
            ShowError(InvalidOtpError);
            return;
        }

        ShowStep(3);
        await CompleteDeviceSetupAsync();
    }

    private async void ResendOtpLinkClicked(object? sender, LinkLabelLinkClickedEventArgs e)
    {
        try
        {
            await _authController.RegisterAsync(
                _emailTextBox.Text.Trim(),
                _displayNameTextBox.Text.Trim(),
                _passwordTextBox.Text);
            StartOtpTimer();
        }
        catch (Exception ex)
        {
            ShowError(ex.Message.Contains("Email is already registered", StringComparison.OrdinalIgnoreCase)
                ? "This email already has an account. Please log in instead."
                : SendOtpFailedError);
        }
    }

    private async Task CompleteDeviceSetupAsync()
    {
        try
        {
            SetSetupStatus(GeneratingKeysStatus, 25);
            await Task.Delay(350);
            SetSetupStatus(RegisteringDeviceStatus, 60);
            var success = await _authController.VerifyOtpAndCompleteRegistrationAsync(
                _emailTextBox.Text.Trim(),
                _otpTextBox.Text.Trim(),
                string.IsNullOrWhiteSpace(_deviceNameTextBox.Text) ? Environment.MachineName : _deviceNameTextBox.Text.Trim());

            if (!success)
            {
                ShowStep(2);
                ShowError(VerifyOtpFailedError);
                return;
            }

            SetSetupStatus(LoggingInStatus, 90);
            await Task.Delay(350);
            SetSetupStatus(LoggingInStatus, 100);
            OpenMainForm();
        }
        catch
        {
            ShowStep(2);
            ShowError(VerifyOtpFailedError);
        }
    }

    private void ShowStep(int step)
    {
        if (InvokeRequired)
        {
            BeginInvoke(() => ShowStep(step));
            return;
        }

        _currentStep = step;
        _stepLabel.Text = step switch
        {
            1 => StepOneText,
            2 => StepTwoText,
            _ => StepThreeText
        };

        _stepOnePanel.Visible = step == 1;
        _stepTwoPanel.Visible = step == 2;
        _stepThreePanel.Visible = step == 3;
        _errorLabel.Visible = false;
        _fadeValue = 30;
        _fadeTimer.Start();
    }

    private void FadeTimerTick(object? sender, EventArgs e)
    {
        _fadeValue += 10;
        var target = _currentStep == 1 ? _stepOnePanel : _currentStep == 2 ? _stepTwoPanel : _stepThreePanel;
        target.BackColor = Color.FromArgb(
            Math.Min(255, _fadeValue),
            ThemeHelper.BgPrimary.R,
            ThemeHelper.BgPrimary.G,
            ThemeHelper.BgPrimary.B);
        target.Invalidate();

        if (_fadeValue >= 100)
        {
            _fadeTimer.Stop();
        }
    }

    private void StartOtpTimer()
    {
        _remainingOtpSeconds = OtpLifetimeSeconds;
        _otpTimer.Start();
        UpdateOtpTimerLabel();
    }

    private void OtpTimerTick(object? sender, EventArgs e)
    {
        _remainingOtpSeconds--;
        if (_remainingOtpSeconds <= 0)
        {
            _otpTimer.Stop();
        }

        UpdateOtpTimerLabel();
    }

    private void UpdateOtpTimerLabel()
    {
        var minutes = Math.Max(0, _remainingOtpSeconds) / 60;
        var seconds = Math.Max(0, _remainingOtpSeconds) % 60;
        _timerLabel.Text = $"{minutes:00}:{seconds:00}";
    }

    private void SetStepOneLoading(bool isLoading)
    {
        _sendOtpButton.Enabled = !isLoading;
        _emailTextBox.Enabled = !isLoading;
        _displayNameTextBox.Enabled = !isLoading;
        _passwordTextBox.Enabled = !isLoading;
    }

    private void SetSetupStatus(string status, int progress)
    {
        if (InvokeRequired)
        {
            BeginInvoke(() => SetSetupStatus(status, progress));
            return;
        }

        _setupStatusLabel.Text = status;
        _setupProgressBar.Value = progress;
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

    private void BackToLoginButtonClick(object? sender, EventArgs e)
    {
        Hide();
        var loginForm = new LoginForm(_serviceProvider);
        loginForm.FormClosed += (_, _) => Close();
        loginForm.Show();
    }
}
