#nullable enable

using SecureChat.Shared.DTOs.Auth;
using SecureChat.Shared.DTOs.Device;

namespace SecureChat.Client.Services.Api;

/// <summary>
/// Provides authentication API operations.
/// </summary>
public class AuthApiService
{
    private readonly IApiClient _apiClient;

    /// <summary>Initializes a new instance of the <see cref="AuthApiService"/> class.</summary>
    public AuthApiService(IApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    /// <summary>Registers a user email.</summary>
    public Task<RegisterResponseDto> RegisterAsync(RegisterRequestDto request) => _apiClient.PostAsync<RegisterRequestDto, RegisterResponseDto>("/api/auth/register", request);

    /// <summary>Verifies an OTP.</summary>
    public Task<bool> VerifyOtpAsync(OtpVerifyRequestDto request) => _apiClient.PostAsync<OtpVerifyRequestDto, bool>("/api/auth/verify-otp", request);

    /// <summary>Completes registration by verifying OTP and registering the first device.</summary>
    public Task<LoginVerifyResponseDto> CompleteRegistrationAsync(CompleteRegistrationRequestDto request) => _apiClient.PostAsync<CompleteRegistrationRequestDto, LoginVerifyResponseDto>("/api/auth/complete-registration", request);

    /// <summary>Gets a login challenge.</summary>
    public Task<LoginChallengeResponseDto> GetChallengeAsync(string deviceId) => _apiClient.GetAsync<LoginChallengeResponseDto>($"/api/auth/challenge/{deviceId}");

    /// <summary>Verifies login.</summary>
    public Task<LoginVerifyResponseDto> LoginAsync(LoginVerifyRequestDto request) => _apiClient.PostAsync<LoginVerifyRequestDto, LoginVerifyResponseDto>("/api/auth/login", request);

    /// <summary>Refreshes tokens.</summary>
    public Task<LoginVerifyResponseDto> RefreshTokenAsync(string refreshToken) => _apiClient.PostAsync<RefreshTokenRequestDto, LoginVerifyResponseDto>("/api/auth/refresh", new RefreshTokenRequestDto { RefreshToken = refreshToken });

    /// <summary>Logs out the current device.</summary>
    public async Task LogoutAsync() => await _apiClient.PostAsync<object, bool>("/api/auth/logout", new { });

    /// <summary>Registers a device.</summary>
    public Task<DeviceDto> RegisterDeviceAsync(RegisterDeviceDto request) => _apiClient.PostAsync<RegisterDeviceDto, DeviceDto>("/api/auth/devices/register", request);

    /// <summary>Gets current user's devices.</summary>
    public async Task<List<DeviceDto>> GetMyDevicesAsync() => (await _apiClient.GetAsync<IReadOnlyCollection<DeviceDto>>("/api/auth/devices")).ToList();

    /// <summary>Revokes a device.</summary>
    public Task<bool> RevokeDeviceAsync(Guid deviceId) => _apiClient.DeleteAsync($"/api/auth/devices/{deviceId}");
}
