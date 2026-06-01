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
        _logger.LogInformation("Registering email {Email}.", request.Email);

        var existingUser = await _userRepository.GetByEmailAsync(request.Email);
        if (string.IsNullOrWhiteSpace(request.DisplayName) || string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 8)
        {
            await LogSecurityEventAsync(null, null, "RegisterRejected", "Invalid registration details");
            throw new DomainException("Display name and a password of at least 8 characters are required.", 400);
        }

        var otpCode = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
        var otpRequest = new OtpRequest(request.Email, otpCode, DateTimeOffset.UtcNow.AddMinutes(10));

        await _otpRepository.AddAsync(otpRequest);
        PendingDisplayNames[request.Email] = request.DisplayName;
        await _emailService.SendOtpAsync(request.Email, otpCode, request.DisplayName);
        await LogSecurityEventAsync(existingUser?.Id, null, "RegisterOtpSent", $"OTP sent to {request.Email}");

        return new RegisterResponseDto
        {
            UserId = Guid.Empty,
            Message = "OTP sent"
        };
    }

    /// <inheritdoc />
    public async Task<bool> VerifyOtpAsync(OtpVerifyRequestDto request)
    {
        _logger.LogInformation("Verifying OTP for email {Email}.", request.Email);

        await VerifyOtpAndCreateUserAsync(request.Email, request.OtpCode);

        return true;
    }

    /// <inheritdoc />
    public async Task<LoginVerifyResponseDto> CompleteRegistrationAsync(CompleteRegistrationRequestDto request)
    {
        _logger.LogInformation("Completing registration for email {Email}.", request.Email);

        var user = await VerifyOtpAndCreateUserAsync(request.Email, request.OtpCode);
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
            await LogSecurityEventAsync(existingUser.Id, null, "DeviceRegistrationOtpVerified", "User verified OTP for device registration");
            return existingUser;
        }

        otpRequest.IsUsed = true;
        await _otpRepository.UpdateAsync(otpRequest);

        var displayName = PendingDisplayNames.TryRemove(email, out var pendingDisplayName)
            ? pendingDisplayName
            : email.Split('@')[0];

        var user = new User(email, displayName);
        await _userRepository.AddAsync(user);
        await LogSecurityEventAsync(user.Id, null, "RegistrationCompleted", "User verified OTP and account was created");

        return user;
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
            IsCurrentDevice = isCurrentDevice,
            Fingerprint = device.DeviceFingerprint
        };
    }
}
