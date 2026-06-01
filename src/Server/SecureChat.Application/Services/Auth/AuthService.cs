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
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(request.DisplayName) || string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 8)
        {
            await LogSecurityEventAsync(null, null, "RegisterRejected", "Invalid registration details");
            throw new DomainException("Display name and a password of at least 8 characters are required.", 400);
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

        var user = new User(NormalizeEmail(email), displayName);
        await _userRepository.AddAsync(user);
        await LogSecurityEventAsync(user.Id, null, "RegistrationCompleted", "User verified OTP and account was created");

        return user;
    }

    private static string NormalizeEmail(string email)
    {
        return (email ?? string.Empty).Trim().ToLowerInvariant();
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
