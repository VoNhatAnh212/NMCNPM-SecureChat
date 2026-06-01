#nullable enable

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecureChat.Application.Services.Auth;
using SecureChat.Application.Services.Device;
using SecureChat.Server.Extensions;
using SecureChat.Server.Responses;
using SecureChat.Shared.DTOs.Auth;
using SecureChat.Shared.DTOs.Device;

namespace SecureChat.Server.Controllers;

/// <summary>
/// Provides authentication and device endpoints.
/// </summary>
[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IDeviceService _deviceService;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthController"/> class.
    /// </summary>
    public AuthController(IAuthService authService, IDeviceService deviceService)
    {
        _authService = authService;
        _deviceService = deviceService;
    }

    /// <summary>Registers an email address and sends an OTP.</summary>
    [HttpPost("register")]
    [ProducesResponseType(typeof(ApiResponse<RegisterResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<RegisterResponseDto>>> RegisterAsync([FromBody] RegisterRequestDto request)
    {
        var response = await _authService.RegisterAsync(request);
        return Ok(ApiResponse<RegisterResponseDto>.Ok(response));
    }

    /// <summary>Verifies an OTP code.</summary>
    [HttpPost("verify-otp")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<bool>>> VerifyOtpAsync([FromBody] OtpVerifyRequestDto request)
    {
        var verified = await _authService.VerifyOtpAsync(request);
        return Ok(ApiResponse<bool>.Ok(verified));
    }

    /// <summary>Verifies OTP, registers the first trusted device, and signs the user in.</summary>
    [HttpPost("complete-registration")]
    [ProducesResponseType(typeof(ApiResponse<LoginVerifyResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<LoginVerifyResponseDto>>> CompleteRegistrationAsync([FromBody] CompleteRegistrationRequestDto request)
    {
        var response = await _authService.CompleteRegistrationAsync(request);
        return Ok(ApiResponse<LoginVerifyResponseDto>.Ok(response));
    }

    /// <summary>Gets a login challenge for a device.</summary>
    [HttpGet("challenge/{deviceId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<LoginChallengeResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<LoginChallengeResponseDto>>> GetChallengeAsync(Guid deviceId)
    {
        var response = await _authService.GetLoginChallengeAsync(new LoginChallengeRequestDto { DeviceId = deviceId });
        return Ok(ApiResponse<LoginChallengeResponseDto>.Ok(response));
    }

    /// <summary>Verifies a signed login challenge.</summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(ApiResponse<LoginVerifyResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<LoginVerifyResponseDto>>> LoginAsync([FromBody] LoginVerifyRequestDto request)
    {
        var response = await _authService.VerifyLoginAsync(request);
        return Ok(ApiResponse<LoginVerifyResponseDto>.Ok(response));
    }

    /// <summary>Refreshes an access token.</summary>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(ApiResponse<LoginVerifyResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<LoginVerifyResponseDto>>> RefreshAsync([FromBody] RefreshTokenRequestDto request)
    {
        var response = await _authService.RefreshTokenAsync(request);
        return Ok(ApiResponse<LoginVerifyResponseDto>.Ok(response));
    }

    /// <summary>Logs out the current device.</summary>
    [Authorize]
    [HttpPost("logout")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<bool>>> LogoutAsync()
    {
        await _authService.LogoutAsync(User.GetDeviceId());
        return Ok(ApiResponse<bool>.Ok(true));
    }

    /// <summary>Registers a new device for the current user.</summary>
    [Authorize]
    [HttpPost("devices/register")]
    [ProducesResponseType(typeof(ApiResponse<DeviceDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<DeviceDto>>> RegisterDeviceAsync([FromBody] RegisterDeviceDto request)
    {
        var response = await _authService.RegisterDeviceAsync(User.GetUserId(), request);
        return Ok(ApiResponse<DeviceDto>.Ok(response));
    }

    /// <summary>Gets devices for the current user.</summary>
    [Authorize]
    [HttpGet("devices")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyCollection<DeviceDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<DeviceDto>>>> GetDevicesAsync()
    {
        var devices = await _deviceService.GetUserDevicesAsync(User.GetUserId(), User.GetDeviceId());
        return Ok(ApiResponse<IReadOnlyCollection<DeviceDto>>.Ok(devices));
    }

    /// <summary>Revokes a device for the current user.</summary>
    [Authorize]
    [HttpDelete("devices/{deviceId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<bool>>> RevokeDeviceAsync(Guid deviceId)
    {
        await _deviceService.RevokeDeviceAsync(User.GetUserId(), deviceId);
        return Ok(ApiResponse<bool>.Ok(true));
    }
}
