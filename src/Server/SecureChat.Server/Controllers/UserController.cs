#nullable enable

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecureChat.Domain.Interfaces.Repositories;
using SecureChat.Server.Extensions;
using SecureChat.Server.Responses;
using SecureChat.Shared.DTOs.User;

namespace SecureChat.Server.Controllers;

/// <summary>
/// Provides user lookup and public-key endpoints.
/// </summary>
[Authorize]
[ApiController]
[Route("api/users")]
public class UserController : ControllerBase
{
    private static readonly TimeSpan OnlineWindow = TimeSpan.FromSeconds(45);
    private readonly IUserRepository _userRepository;
    private readonly IPublicKeyRepository _publicKeyRepository;

    /// <summary>Initializes a new instance of the <see cref="UserController"/> class.</summary>
    public UserController(IUserRepository userRepository, IPublicKeyRepository publicKeyRepository)
    {
        _userRepository = userRepository;
        _publicKeyRepository = publicKeyRepository;
    }

    /// <summary>Updates the current user's presence heartbeat.</summary>
    [HttpPost("presence/ping")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<bool>>> PingPresence()
    {
        var user = await _userRepository.GetByIdAsync(User.GetUserId());
        if (user is not null)
        {
            user.IsOnline = true;
            user.LastSeen = DateTimeOffset.UtcNow;
            await _userRepository.UpdateAsync(user);
        }

        return Ok(ApiResponse<bool>.Ok(true));
    }

    /// <summary>Searches users by display name or email.</summary>
    [HttpGet("search")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyCollection<UserDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<UserDto>>>> SearchUsers([FromQuery] string q, [FromQuery] int limit = 20)
    {
        var currentUserId = User.GetUserId();
        var users = await _userRepository.SearchAsync(q ?? string.Empty, limit + 1);
        var result = users
            .Where(user => user.Id != currentUserId)
            .Take(Math.Max(1, limit))
            .Select(user => new UserDto
            {
                Id = user.Id,
                DisplayName = user.DisplayName,
                Email = user.Email,
                AvatarUrl = user.AvatarUrl,
                IsOnline = IsCurrentlyOnline(user),
                LastSeen = user.LastSeen
            })
            .ToList();

        return Ok(ApiResponse<IReadOnlyCollection<UserDto>>.Ok(result));
    }

    /// <summary>Gets active public keys for a user's devices.</summary>
    [HttpGet("{userId:guid}/public-keys")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyCollection<PublicKeyDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<PublicKeyDto>>>> GetPublicKeys(Guid userId)
    {
        var keys = await _publicKeyRepository.GetByUserIdAsync(userId);
        var result = keys
            .Where(key => key.IsActive && key.Device.Status == SecureChat.Shared.Enums.DeviceStatus.Active)
            .Select(key => new PublicKeyDto
            {
                UserId = key.UserId,
                DeviceId = key.DeviceId,
                PublicKeyPem = key.PublicKeyPem,
                CreatedAt = key.CreatedAt
            })
            .ToList();

        return Ok(ApiResponse<IReadOnlyCollection<PublicKeyDto>>.Ok(result));
    }

    private static bool IsCurrentlyOnline(SecureChat.Domain.Entities.User user)
    {
        return user.IsOnline && user.LastSeen is not null && DateTimeOffset.UtcNow - user.LastSeen.Value <= OnlineWindow;
    }
}
