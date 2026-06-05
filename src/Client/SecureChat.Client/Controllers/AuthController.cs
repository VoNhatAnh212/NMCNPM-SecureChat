#nullable enable

using System.Security.Cryptography;
using System.Text;
using SecureChat.Client.Models;
using SecureChat.Client.Services;
using SecureChat.Client.Services.Api;
using SecureChat.Client.Services.Crypto;
using SecureChat.Client.Services.SignalR;
using SecureChat.Client.Services.Storage;
using SecureChat.Shared.DTOs.Auth;
using SecureChat.Shared.DTOs.Device;

namespace SecureChat.Client.Controllers;

/// <summary>
/// Orchestrates client authentication workflows.
/// </summary>
public class AuthController
{
    private readonly AuthApiService _authApiService;
    private readonly RsaService _rsaService;
    private readonly KeyStorageService _keyStorageService;
    private readonly AuthSessionService _authSession;
    private readonly ISignalRService _signalRService;
    private readonly SessionCache _sessionCache;

    /// <summary>Represents the result of starting a password login flow.</summary>
    public enum PasswordLoginStartStatus
    {
        /// <summary>The account was logged in through an existing trusted local device key.</summary>
        LoggedIn,

        /// <summary>The account needs OTP verification before this device can be trusted.</summary>
        OtpRequired
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthController"/> class.
    /// </summary>
    public AuthController(
        AuthApiService authApiService,
        RsaService rsaService,
        KeyStorageService keyStorageService,
        AuthSessionService authSession,
        ISignalRService signalRService,
        SessionCache sessionCache)
    {
        _authApiService = authApiService;
        _rsaService = rsaService;
        _keyStorageService = keyStorageService;
        _authSession = authSession;
        _signalRService = signalRService;
        _sessionCache = sessionCache;
    }

    /// <summary>Logs in an existing registered device.</summary>
    public async Task<bool> LoginAsync(string deviceId)
    {
        var privateKeyPem = await _keyStorageService.LoadPrivateKeyAsync(deviceId);
        if (string.IsNullOrWhiteSpace(privateKeyPem))
        {
            return false;
        }

        var challenge = await _authApiService.GetChallengeAsync(deviceId);
        var signedChallenge = await _rsaService.SignChallengeAsync(challenge.Challenge, privateKeyPem);
        var loginResponse = await _authApiService.LoginAsync(new LoginVerifyRequestDto
        {
            DeviceId = Guid.Parse(deviceId),
            SignedChallenge = signedChallenge
        });

        var account = new LocalUser
        {
            Id = loginResponse.User.Id,
            DisplayName = loginResponse.User.DisplayName,
            Email = loginResponse.User.Email,
            AvatarUrl = loginResponse.User.AvatarUrl,
            DeviceId = deviceId,
            LastLoginAt = DateTime.UtcNow
        };
        await _keyStorageService.SaveTokensAsync(loginResponse.AccessToken, loginResponse.RefreshToken, loginResponse.ExpiresAt.DateTime, account);
        await _keyStorageService.SaveAccountAsync(account);
        _authSession.SetSession(loginResponse.AccessToken, loginResponse.RefreshToken, loginResponse.ExpiresAt.DateTime, account);

        await _signalRService.ConnectAsync(loginResponse.AccessToken);
        return true;
    }

    /// <summary>Starts login by username and password hash, using a trusted local key when available.</summary>
    public async Task<PasswordLoginStartStatus> StartPasswordLoginAsync(string username, string password)
    {
        var normalizedUsername = NormalizeUsername(username);
        var passwordHash = HashPasswordForTransport(password);
        var accounts = await _keyStorageService.LoadAccountsAsync();
        var localAccount = accounts
            .OrderByDescending(account => account.LastLoginAt)
            .FirstOrDefault(account => string.Equals(account.Email, normalizedUsername, StringComparison.OrdinalIgnoreCase));

        if (localAccount is not null)
        {
            var privateKeyPem = await _keyStorageService.LoadPrivateKeyAsync(localAccount.DeviceId);
            if (!string.IsNullOrWhiteSpace(privateKeyPem))
            {
                var challenge = await _authApiService.GetPasswordChallengeAsync(new PasswordLoginChallengeRequestDto
                {
                    Username = normalizedUsername,
                    PasswordHash = passwordHash,
                    DeviceId = Guid.Parse(localAccount.DeviceId)
                });
                var signedChallenge = await _rsaService.SignChallengeAsync(challenge.Challenge, privateKeyPem);
                var loginResponse = await _authApiService.LoginAsync(new LoginVerifyRequestDto
                {
                    DeviceId = Guid.Parse(localAccount.DeviceId),
                    SignedChallenge = signedChallenge
                });

                await StoreLoginResponseAsync(loginResponse, localAccount.DeviceId);
                return PasswordLoginStartStatus.LoggedIn;
            }
        }

        await _authApiService.StartPasswordLoginAsync(new PasswordLoginStartRequestDto
        {
            Username = normalizedUsername,
            PasswordHash = passwordHash
        });
        return PasswordLoginStartStatus.OtpRequired;
    }

    /// <summary>Completes password login on a device that must become trusted through OTP.</summary>
    public async Task<bool> CompletePasswordDeviceLoginAsync(string username, string password, string otp, string deviceName)
    {
        var keyPair = await _rsaService.GenerateKeyPairAsync();
        var fingerprint = await _rsaService.GetPublicKeyFingerprintAsync(keyPair.publicKeyPem);
        var loginResponse = await _authApiService.CompletePasswordDeviceLoginAsync(new CompletePasswordDeviceLoginRequestDto
        {
            Username = NormalizeUsername(username),
            PasswordHash = HashPasswordForTransport(password),
            OtpCode = otp,
            Device = new RegisterDeviceDto
            {
                DeviceName = string.IsNullOrWhiteSpace(deviceName) ? Environment.MachineName : deviceName.Trim(),
                Platform = Environment.OSVersion.Platform.ToString(),
                PublicKeyPem = keyPair.publicKeyPem,
                DeviceFingerprint = fingerprint
            }
        });

        var deviceId = ExtractDeviceId(loginResponse.AccessToken);
        await _keyStorageService.SavePrivateKeyAsync(deviceId, keyPair.privateKeyPem);
        await StoreLoginResponseAsync(loginResponse, deviceId);
        return true;
    }

    /// <summary>Starts registration by sending an OTP.</summary>
    public async Task<bool> RegisterAsync(string email, string displayName, string password)
    {
        var response = await _authApiService.RegisterAsync(new RegisterRequestDto
        {
            Email = email,
            DisplayName = displayName,
            Password = HashPasswordForTransport(password)
        });

        return string.Equals(response.Message, "OTP sent", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Verifies OTP, registers a device, saves the private key, and logs in.</summary>
    public async Task<bool> VerifyOtpAndCompleteRegistrationAsync(string email, string otp, string deviceName)
    {
        var keyPair = await _rsaService.GenerateKeyPairAsync();
        var fingerprint = await _rsaService.GetPublicKeyFingerprintAsync(keyPair.publicKeyPem);
        var loginResponse = await _authApiService.CompleteRegistrationAsync(new CompleteRegistrationRequestDto
        {
            Email = email,
            OtpCode = otp,
            Device = new RegisterDeviceDto
            {
                DeviceName = deviceName,
                Platform = Environment.OSVersion.Platform.ToString(),
                PublicKeyPem = keyPair.publicKeyPem,
                DeviceFingerprint = fingerprint
            }
        });

        var deviceId = ExtractDeviceId(loginResponse.AccessToken);
        await _keyStorageService.SavePrivateKeyAsync(deviceId, keyPair.privateKeyPem);
        var account = new LocalUser
        {
            Id = loginResponse.User.Id,
            DisplayName = loginResponse.User.DisplayName,
            Email = loginResponse.User.Email,
            AvatarUrl = loginResponse.User.AvatarUrl,
            DeviceId = deviceId,
            LastLoginAt = DateTime.UtcNow
        };
        await _keyStorageService.SaveTokensAsync(loginResponse.AccessToken, loginResponse.RefreshToken, loginResponse.ExpiresAt.DateTime, account);
        await _keyStorageService.SaveAccountAsync(account);
        _authSession.SetSession(loginResponse.AccessToken, loginResponse.RefreshToken, loginResponse.ExpiresAt.DateTime, account);

        await _signalRService.ConnectAsync(loginResponse.AccessToken);
        return true;
    }

    private async Task StoreLoginResponseAsync(LoginVerifyResponseDto loginResponse, string deviceId)
    {
        var account = new LocalUser
        {
            Id = loginResponse.User.Id,
            DisplayName = loginResponse.User.DisplayName,
            Email = loginResponse.User.Email,
            AvatarUrl = loginResponse.User.AvatarUrl,
            DeviceId = deviceId,
            LastLoginAt = DateTime.UtcNow
        };
        await _keyStorageService.SaveTokensAsync(loginResponse.AccessToken, loginResponse.RefreshToken, loginResponse.ExpiresAt.DateTime, account);
        await _keyStorageService.SaveAccountAsync(account);
        _authSession.SetSession(loginResponse.AccessToken, loginResponse.RefreshToken, loginResponse.ExpiresAt.DateTime, account);
        await _signalRService.ConnectAsync(loginResponse.AccessToken);
    }

    private static string HashPasswordForTransport(string password)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static string NormalizeUsername(string username)
    {
        return (username ?? string.Empty).Trim().ToLowerInvariant();
    }

    /// <summary>Logs out the current device.</summary>
    public async Task LogoutAsync()
    {
        await _authApiService.LogoutAsync();
        await _signalRService.DisconnectAsync();
        _sessionCache.ClearAll();
        var account = _authSession.CurrentAccount;
        _authSession.Clear();
        await _keyStorageService.ClearTokensAsync(account);
    }

    private static string ExtractDeviceId(string accessToken)
    {
        var parts = accessToken.Split('.');
        if (parts.Length < 2)
        {
            throw new InvalidOperationException("Access token is malformed.");
        }

        var payload = parts[1].Replace('-', '+').Replace('_', '/');
        payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');
        var json = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(payload));
        using var document = System.Text.Json.JsonDocument.Parse(json);
        return document.RootElement.TryGetProperty("deviceId", out var deviceId)
            ? deviceId.GetString() ?? throw new InvalidOperationException("Device id claim is empty.")
            : throw new InvalidOperationException("Device id claim is missing.");
    }
}
