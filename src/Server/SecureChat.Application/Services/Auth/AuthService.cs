#nullable enable

using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using SecureChat.Domain.Entities;
using SecureChat.Domain.Exceptions;
using SecureChat.Domain.Interfaces.Repositories;
using SecureChat.Domain.Interfaces.Services;
using SecureChat.Shared.Constants;
using SecureChat.Shared.DTOs.Auth;
using SecureChat.Shared.DTOs.Device;
using SecureChat.Shared.DTOs.User;
using SecureChat.Shared.Enums;
using DomainDevice = SecureChat.Domain.Entities.Device;

namespace SecureChat.Application.Services.Auth;

/// <summary>
/// Provides authentication application workflows.
/// </summary>
public class AuthService : IAuthService
{
    private static readonly ConcurrentDictionary<string, string> PendingDisplayNames = new(StringComparer.OrdinalIgnoreCase);
    private static readonly ConcurrentDictionary<string, string> PendingPasswordHashes = new(StringComparer.OrdinalIgnoreCase);
    private const int PasswordSaltSizeBytes = 16;
    private const int PasswordHashSizeBytes = 32;
    private const int PasswordIterations = 150_000;

    private readonly IUserRepository _userRepository;
    private readonly IDeviceRepository _deviceRepository;
    private readonly IPublicKeyRepository _publicKeyRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IOtpRepository _otpRepository;
    private readonly IRepository<SecurityLog> _securityLogRepository;
    private readonly IJwtService _jwtService;
    private readonly IChallengeService _challengeService;
    private readonly IEmailService _emailService;
    private readonly ILogger<AuthService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthService"/> class.
    /// </summary>
    public AuthService(
        IUserRepository userRepository,
        IDeviceRepository deviceRepository,
        IPublicKeyRepository publicKeyRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IOtpRepository otpRepository,
        IRepository<SecurityLog> securityLogRepository,
        IJwtService jwtService,
        IChallengeService challengeService,
        IEmailService emailService,
        ILogger<AuthService> logger)
    {
        _userRepository = userRepository;
        _deviceRepository = deviceRepository;
        _publicKeyRepository = publicKeyRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _otpRepository = otpRepository;
        _securityLogRepository = securityLogRepository;
        _jwtService = jwtService;
        _challengeService = challengeService;
        _emailService = emailService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<RegisterResponseDto> RegisterAsync(RegisterRequestDto request)
    {
        var email = NormalizeEmail(request.Email);
        _logger.LogInformation("Registering email {Email}.", email);

        var existingUser = await _userRepository.GetByEmailAsync(email);
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(request.DisplayName) || !IsClientPasswordHash(request.Password))
        {
            await LogSecurityEventAsync(null, null, "RegisterRejected", "Invalid registration details");
            throw new DomainException("Display name and a valid password hash are required.", 400);
        }

        if (existingUser is not null)
        {
            await LogSecurityEventAsync(existingUser.Id, null, "RegisterRejected", "Email is already registered");
            throw new DomainException("Email is already registered.", 409);
        }

        var otpCode = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
        var otpRequest = new OtpRequest(email, otpCode, DateTimeOffset.UtcNow.AddMinutes(10));

        await _otpRepository.AddAsync(otpRequest);
        PendingDisplayNames[email] = request.DisplayName.Trim();
        PendingPasswordHashes[email] = request.Password.Trim();
        await _emailService.SendOtpAsync(email, otpCode, request.DisplayName.Trim());
        await LogSecurityEventAsync(null, null, "RegisterOtpSent", $"OTP sent to {email}");

        return new RegisterResponseDto
        {
            UserId = Guid.Empty,
            Message = "OTP sent"
        };
    }

    /// <inheritdoc />
    public async Task<bool> VerifyOtpAsync(OtpVerifyRequestDto request)
    {
        var email = NormalizeEmail(request.Email);
        _logger.LogInformation("Verifying OTP for email {Email}.", email);

        await VerifyOtpAndCreateUserAsync(email, request.OtpCode);

        return true;
    }

    /// <inheritdoc />
    public async Task<LoginVerifyResponseDto> CompleteRegistrationAsync(CompleteRegistrationRequestDto request)
    {
        var email = NormalizeEmail(request.Email);
        _logger.LogInformation("Completing registration for email {Email}.", email);

        var user = await VerifyOtpAndCreateUserAsync(email, request.OtpCode);
        var existingDevice = await _deviceRepository.GetByFingerprintAsync(request.Device.DeviceFingerprint);
        if (existingDevice is not null)
        {
            throw new DomainException("Device fingerprint is already registered.", 409);
        }

        var device = new DomainDevice(
            user.Id,
            request.Device.DeviceName,
            request.Device.Platform,
            request.Device.DeviceFingerprint);

        await _deviceRepository.AddAsync(device);

        var publicKey = new PublicKey(user.Id, device.Id, request.Device.PublicKeyPem, ComputeFingerprint(request.Device.PublicKeyPem));
        await _publicKeyRepository.AddAsync(publicKey);

        device.LastLoginAt = DateTimeOffset.UtcNow;
        await _deviceRepository.UpdateAsync(device);

        await LogSecurityEventAsync(user.Id, device.Id, "DeviceRegistered", request.Device.DeviceName);
        await _emailService.SendSecurityAlertAsync(user.Email, "NewDeviceRegistered", request.Device.DeviceName, "unknown");

        return await IssueTokensAsync(user, device);
    }

    /// <inheritdoc />
    public async Task<PasswordLoginStartResponseDto> StartPasswordLoginAsync(PasswordLoginStartRequestDto request)
    {
        var username = NormalizeEmail(request.Username);
        var user = await ValidatePasswordLoginAsync(username, request.PasswordHash);
        var otpCode = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
        var otpRequest = new OtpRequest(user.Email, otpCode, DateTimeOffset.UtcNow.AddMinutes(10));

        await _otpRepository.AddAsync(otpRequest);
        await _emailService.SendOtpAsync(user.Email, otpCode, user.DisplayName);
        await LogSecurityEventAsync(user.Id, null, "PasswordLoginOtpSent", "OTP sent for new device login");

        return new PasswordLoginStartResponseDto
        {
            RequiresOtp = true,
            Email = user.Email,
            DisplayName = user.DisplayName,
            Message = "OTP sent"
        };
    }

    /// <inheritdoc />
    public async Task<LoginVerifyResponseDto> CompletePasswordDeviceLoginAsync(CompletePasswordDeviceLoginRequestDto request)
    {
        var username = NormalizeEmail(request.Username);
        var user = await ValidatePasswordLoginAsync(username, request.PasswordHash);
        var otpRequest = await _otpRepository.GetValidOtpAsync(user.Email, request.OtpCode);
        if (otpRequest is null)
        {
            await LogSecurityEventAsync(user.Id, null, "PasswordLoginOtpRejected", "Invalid or expired new device OTP");
            throw new OtpExpiredException();
        }

        if (otpRequest.Attempts >= 5)
        {
            await LogSecurityEventAsync(user.Id, null, "PasswordLoginOtpAttemptLimitExceeded", user.Email);
            throw new UnauthorizedException("OTP attempt limit exceeded.");
        }

        otpRequest.IsUsed = true;
        await _otpRepository.UpdateAsync(otpRequest);

        var existingDevice = await _deviceRepository.GetByFingerprintAsync(request.Device.DeviceFingerprint);
        if (existingDevice is not null)
        {
            throw new DomainException("Device fingerprint is already registered.", 409);
        }

        var device = new DomainDevice(user.Id, request.Device.DeviceName, request.Device.Platform, request.Device.DeviceFingerprint);
        await _deviceRepository.AddAsync(device);

        var publicKey = new PublicKey(user.Id, device.Id, request.Device.PublicKeyPem, ComputeFingerprint(request.Device.PublicKeyPem));
        await _publicKeyRepository.AddAsync(publicKey);

        device.LastLoginAt = DateTimeOffset.UtcNow;
        await _deviceRepository.UpdateAsync(device);

        await _emailService.SendSecurityAlertAsync(user.Email, "NewDeviceRegisteredByPasswordLogin", request.Device.DeviceName, "unknown");
        await LogSecurityEventAsync(user.Id, device.Id, "PasswordDeviceLoginSucceeded", "Password and OTP verified; new device registered");

        return await IssueTokensAsync(user, device);
    }

    /// <inheritdoc />
    public async Task<LoginChallengeResponseDto> GetPasswordLoginChallengeAsync(PasswordLoginChallengeRequestDto request)
    {
        var username = NormalizeEmail(request.Username);
        var user = await ValidatePasswordLoginAsync(username, request.PasswordHash);
        var device = await GetActiveDeviceAsync(request.DeviceId);
        if (device.UserId != user.Id)
        {
            await LogSecurityEventAsync(user.Id, device.Id, "PasswordLoginDeviceRejected", "Device does not belong to username");
            throw new UnauthorizedException("Device does not belong to this account.");
        }

        var challenge = await _challengeService.GenerateChallengeAsync(device.Id);
        await LogSecurityEventAsync(user.Id, device.Id, "PasswordLoginChallengeGenerated", "Password verified before login challenge");

        return new LoginChallengeResponseDto
        {
            Challenge = challenge,
            ExpiresAt = DateTimeOffset.UtcNow.AddSeconds(AppConstants.CHALLENGE_EXPIRY_SECONDS)
        };
    }

    /// <inheritdoc />
    public async Task<LoginChallengeResponseDto> GetLoginChallengeAsync(LoginChallengeRequestDto request)
    {
        _logger.LogInformation("Getting login challenge for device {DeviceId}.", request.DeviceId);

        var device = await GetActiveDeviceAsync(request.DeviceId);
        var challenge = await _challengeService.GenerateChallengeAsync(device.Id);

        await LogSecurityEventAsync(device.UserId, device.Id, "LoginChallengeGenerated", "Login challenge generated");

        return new LoginChallengeResponseDto
        {
            Challenge = challenge,
            ExpiresAt = DateTimeOffset.UtcNow.AddSeconds(AppConstants.CHALLENGE_EXPIRY_SECONDS)
        };
    }

    /// <inheritdoc />
    public async Task<LoginVerifyResponseDto> VerifyLoginAsync(LoginVerifyRequestDto request)
    {
        _logger.LogInformation("Verifying login for device {DeviceId}.", request.DeviceId);

        var device = await GetActiveDeviceAsync(request.DeviceId);
        var user = await _userRepository.GetByIdAsync(device.UserId) ?? throw new UserNotFoundException(device.UserId);
        var publicKey = await _publicKeyRepository.GetActiveByDeviceIdAsync(device.Id)
            ?? throw new InvalidSignatureException();

        var isValid = await _challengeService.ValidateChallengeAsync(device.Id, request.SignedChallenge, publicKey.PublicKeyPem);
        if (!isValid)
        {
            await LogSecurityEventAsync(user.Id, device.Id, "LoginSignatureRejected", "Invalid login signature");
            throw new InvalidSignatureException();
        }

        var response = await IssueTokensAsync(user, device);
        device.LastLoginAt = DateTimeOffset.UtcNow;
        await _deviceRepository.UpdateAsync(device);
        await LogSecurityEventAsync(user.Id, device.Id, "LoginSucceeded", "Device challenge verified");

        return response;
    }

    /// <inheritdoc />
    public async Task<LoginVerifyResponseDto> RefreshTokenAsync(RefreshTokenRequestDto request)
    {
        _logger.LogInformation("Refreshing access token.");

        var existingToken = await _refreshTokenRepository.GetActiveTokenAsync(request.RefreshToken);
        if (existingToken is null)
        {
            throw new UnauthorizedException("Refresh token is invalid or expired.");
        }

        existingToken.IsRevoked = true;
        existingToken.RevokedAt = DateTimeOffset.UtcNow;
        await _refreshTokenRepository.UpdateAsync(existingToken);

        var user = existingToken.User;
        var device = existingToken.Device;

        if (device.Status != DeviceStatus.Active)
        {
            throw new DeviceRevokedException(device.Id);
        }

        await LogSecurityEventAsync(user.Id, device.Id, "TokenRefreshed", "Refresh token rotated");
        return await IssueTokensAsync(user, device);
    }

    /// <inheritdoc />
    public async Task LogoutAsync(Guid deviceId)
    {
        _logger.LogInformation("Logging out device {DeviceId}.", deviceId);

        var device = await _deviceRepository.GetByIdAsync(deviceId) ?? throw new DeviceNotFoundException(deviceId);
        await _refreshTokenRepository.RevokeAllForDeviceAsync(deviceId);
        await LogSecurityEventAsync(device.UserId, device.Id, "Logout", "Refresh tokens revoked for device");
    }

    /// <inheritdoc />
    public async Task LogoutAllDevicesAsync(Guid userId)
    {
        _logger.LogInformation("Logging out all devices for user {UserId}.", userId);

        await _refreshTokenRepository.RevokeAllForUserAsync(userId);
        await LogSecurityEventAsync(userId, null, "LogoutAllDevices", "All refresh tokens revoked for user");
    }

    /// <inheritdoc />
    public async Task<DeviceDto> RegisterDeviceAsync(Guid userId, RegisterDeviceDto request)
    {
        _logger.LogInformation("Registering device for user {UserId}.", userId);

        var user = await _userRepository.GetByIdAsync(userId) ?? throw new UserNotFoundException(userId);
        var existingDevice = await _deviceRepository.GetByFingerprintAsync(request.DeviceFingerprint);
        if (existingDevice is not null)
        {
            throw new DomainException("Device fingerprint is already registered.", 409);
        }

        var device = new DomainDevice(userId, request.DeviceName, request.Platform, request.DeviceFingerprint);
        await _deviceRepository.AddAsync(device);

        var publicKey = new PublicKey(userId, device.Id, request.PublicKeyPem, ComputeFingerprint(request.PublicKeyPem));
        await _publicKeyRepository.AddAsync(publicKey);

        await _emailService.SendSecurityAlertAsync(user.Email, "NewDeviceRegistered", request.DeviceName, "unknown");
        await LogSecurityEventAsync(user.Id, device.Id, "DeviceRegistered", request.DeviceName);

        return ToDeviceDto(device, true);
    }

    /// <inheritdoc />
    public async Task<DeviceDto> RotateCurrentDeviceKeyAsync(Guid userId, Guid deviceId, RegisterDeviceDto request)
    {
        _logger.LogInformation("Rotating key for device {DeviceId}.", deviceId);

        var device = await GetActiveDeviceAsync(deviceId);
        if (device.UserId != userId)
        {
            throw new UnauthorizedException("The device does not belong to the current user.");
        }

        var publicKey = await _publicKeyRepository.GetActiveByDeviceIdAsync(device.Id);
        if (publicKey is null)
        {
            publicKey = new PublicKey(userId, device.Id, request.PublicKeyPem, ComputeFingerprint(request.PublicKeyPem));
            await _publicKeyRepository.AddAsync(publicKey);
        }
        else
        {
            publicKey.PublicKeyPem = request.PublicKeyPem;
            publicKey.KeyFingerprint = ComputeFingerprint(request.PublicKeyPem);
            publicKey.IsActive = true;
            publicKey.CreatedAt = DateTimeOffset.UtcNow;
            await _publicKeyRepository.UpdateAsync(publicKey);
        }

        if (!string.IsNullOrWhiteSpace(request.DeviceName))
        {
            device.DeviceName = request.DeviceName;
        }

        device.DeviceFingerprint = request.DeviceFingerprint;
        await _deviceRepository.UpdateAsync(device);
        await LogSecurityEventAsync(userId, device.Id, "DeviceKeyRotated", "Current device public key rotated");
        return ToDeviceDto(device, true);
    }

    private async Task<LoginVerifyResponseDto> IssueTokensAsync(User user, DomainDevice device)
    {
        var accessToken = await _jwtService.GenerateAccessTokenAsync(user, device);
        var refreshToken = await _jwtService.GenerateRefreshTokenAsync(user.Id, device.Id);

        return new LoginVerifyResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken.Token,
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(AppConstants.TOKEN_EXPIRY_MINUTES),
            User = ToUserDto(user)
        };
    }

    private async Task<User> VerifyOtpAndCreateUserAsync(string email, string otpCode)
    {
        var otpRequest = await _otpRepository.GetValidOtpAsync(email, otpCode);
        if (otpRequest is null)
        {
            await LogSecurityEventAsync(null, null, "OtpVerificationFailed", $"Invalid or expired OTP for {email}");
            throw new OtpExpiredException();
        }

        if (otpRequest.Attempts >= 5)
        {
            await LogSecurityEventAsync(null, null, "OtpAttemptLimitExceeded", email);
            throw new UnauthorizedException("OTP attempt limit exceeded.");
        }

        var existingUser = await _userRepository.GetByEmailAsync(email);
        if (existingUser is not null)
        {
            otpRequest.IsUsed = true;
            await _otpRepository.UpdateAsync(otpRequest);
            await LogSecurityEventAsync(existingUser.Id, null, "RegistrationRejected", "Email is already registered");
            throw new DomainException("Email is already registered.", 409);
        }

        otpRequest.IsUsed = true;
        await _otpRepository.UpdateAsync(otpRequest);

        var displayName = PendingDisplayNames.TryRemove(email, out var pendingDisplayName)
            ? pendingDisplayName
            : email.Split('@')[0];
        PendingPasswordHashes.TryRemove(email, out var clientPasswordHash);

        var user = new User(NormalizeEmail(email), displayName);
        SetPasswordVerifier(user, clientPasswordHash ?? string.Empty);
        await _userRepository.AddAsync(user);
        await LogSecurityEventAsync(user.Id, null, "RegistrationCompleted", "User verified OTP and account was created");

        return user;
    }

    private static string NormalizeEmail(string email)
    {
        return (email ?? string.Empty).Trim().ToLowerInvariant();
    }

    private async Task<User> ValidatePasswordLoginAsync(string username, string clientPasswordHash)
    {
        if (!IsClientPasswordHash(clientPasswordHash))
        {
            await LogSecurityEventAsync(null, null, "PasswordLoginRejected", "Invalid password hash format");
            throw new UnauthorizedException("Invalid username or password.");
        }

        var user = await _userRepository.GetByEmailAsync(username);
        if (user is null || string.IsNullOrWhiteSpace(user.PasswordHash) || string.IsNullOrWhiteSpace(user.PasswordSalt) || !VerifyPasswordHash(clientPasswordHash, user.PasswordSalt, user.PasswordHash))
        {
            await LogSecurityEventAsync(user?.Id, null, "PasswordLoginRejected", "Invalid username or password");
            throw new UnauthorizedException("Invalid username or password.");
        }

        return user;
    }

    private static bool IsClientPasswordHash(string value)
    {
        return !string.IsNullOrWhiteSpace(value) && value.Length == 64 && value.All(Uri.IsHexDigit);
    }

    private static void SetPasswordVerifier(User user, string clientPasswordHash)
    {
        if (!IsClientPasswordHash(clientPasswordHash))
        {
            return;
        }

        var salt = RandomNumberGenerator.GetBytes(PasswordSaltSizeBytes);
        var hash = DerivePasswordHash(clientPasswordHash, salt);
        user.PasswordSalt = Convert.ToBase64String(salt);
        user.PasswordHash = Convert.ToBase64String(hash);
    }

    private static bool VerifyPasswordHash(string clientPasswordHash, string saltBase64, string expectedHashBase64)
    {
        try
        {
            var salt = Convert.FromBase64String(saltBase64);
            var expectedHash = Convert.FromBase64String(expectedHashBase64);
            var actualHash = DerivePasswordHash(clientPasswordHash, salt);
            return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static byte[] DerivePasswordHash(string clientPasswordHash, byte[] salt)
    {
        return Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(clientPasswordHash.ToLowerInvariant()),
            salt,
            PasswordIterations,
            HashAlgorithmName.SHA256,
            PasswordHashSizeBytes);
    }

    private async Task<DomainDevice> GetActiveDeviceAsync(Guid deviceId)
    {
        var device = await _deviceRepository.GetByIdAsync(deviceId) ?? throw new DeviceNotFoundException(deviceId);
        if (device.Status != DeviceStatus.Active)
        {
            throw new DeviceRevokedException(deviceId);
        }

        return device;
    }

    private async Task LogSecurityEventAsync(Guid? userId, Guid? deviceId, string eventType, string details)
    {
        var securityLog = new SecurityLog(userId, deviceId, eventType)
        {
            Details = details
        };

        await _securityLogRepository.AddAsync(securityLog);
    }

    private static string ComputeFingerprint(string publicKeyPem)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(publicKeyPem));
        return Convert.ToHexString(hash);
    }

    private static UserDto ToUserDto(User user)
    {
        return new UserDto
        {
            Id = user.Id,
            DisplayName = user.DisplayName,
            Email = user.Email,
            AvatarUrl = user.AvatarUrl,
            IsOnline = user.IsOnline,
            LastSeen = user.LastSeen
        };
    }

    private static DeviceDto ToDeviceDto(DomainDevice device, bool isCurrentDevice)
    {
        return new DeviceDto
        {
            Id = device.Id,
            DeviceName = device.DeviceName,
            Platform = device.Platform,
            LastLoginAt = device.LastLoginAt,
            CreatedAt = device.CreatedAt,
            IsCurrentDevice = isCurrentDevice,
            Fingerprint = device.DeviceFingerprint,
            Status = device.Status
        };
    }
}
