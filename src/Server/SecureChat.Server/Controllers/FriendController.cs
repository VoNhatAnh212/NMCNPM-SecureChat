#nullable enable

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecureChat.Application.Services.Friend;
using SecureChat.Server.Extensions;
using SecureChat.Server.Responses;
using SecureChat.Shared.DTOs.Friend;

namespace SecureChat.Server.Controllers;

/// <summary>
/// Provides friend API endpoints.
/// </summary>
[Authorize]
[ApiController]
[Route("api/friends")]
public class FriendController : ControllerBase
{
    private readonly IFriendService _friendService;

    /// <summary>Initializes a new instance of the <see cref="FriendController"/> class.</summary>
    public FriendController(IFriendService friendService)
    {
        _friendService = friendService;
    }

    /// <summary>Gets friends for the current user.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyCollection<FriendDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<FriendDto>>>> GetFriends()
    {
        var friends = await _friendService.GetFriendsAsync(User.GetUserId());
        return Ok(ApiResponse<IReadOnlyCollection<FriendDto>>.Ok(friends));
    }

    /// <summary>Gets pending friend requests.</summary>
    [HttpGet("requests")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyCollection<FriendRequestDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<FriendRequestDto>>>> GetPendingRequests()
    {
        var requests = await _friendService.GetPendingRequestsAsync(User.GetUserId());
        return Ok(ApiResponse<IReadOnlyCollection<FriendRequestDto>>.Ok(requests));
    }

    /// <summary>Gets sent pending friend requests.</summary>
    [HttpGet("requests/sent")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyCollection<FriendRequestDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<FriendRequestDto>>>> GetSentPendingRequests()
    {
        var requests = await _friendService.GetSentPendingRequestsAsync(User.GetUserId());
        return Ok(ApiResponse<IReadOnlyCollection<FriendRequestDto>>.Ok(requests));
    }

    /// <summary>Sends a friend request.</summary>
    [HttpPost("requests")]
    [ProducesResponseType(typeof(ApiResponse<FriendRequestDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<FriendRequestDto>>> SendFriendRequest([FromBody] SendFriendRequestBody body)
    {
        var request = await _friendService.SendFriendRequestAsync(User.GetUserId(), body.ToUserId, body.Message);
        return Ok(ApiResponse<FriendRequestDto>.Ok(request));
    }

    /// <summary>Accepts a friend request.</summary>
    [HttpPut("requests/{id:guid}/accept")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<bool>>> AcceptRequest(Guid id)
    {
        var accepted = await _friendService.RespondToRequestAsync(id, User.GetUserId(), true);
        return Ok(ApiResponse<bool>.Ok(accepted));
    }

    /// <summary>Rejects a friend request.</summary>
    [HttpPut("requests/{id:guid}/reject")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<bool>>> RejectRequest(Guid id)
    {
        var accepted = await _friendService.RespondToRequestAsync(id, User.GetUserId(), false);
        return Ok(ApiResponse<bool>.Ok(accepted));
    }

    /// <summary>Removes a friend.</summary>
    [HttpDelete("{friendId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<bool>>> RemoveFriend(Guid friendId)
    {
        await _friendService.RemoveFriendAsync(User.GetUserId(), friendId);
        return Ok(ApiResponse<bool>.Ok(true));
    }

    /// <summary>Blocks a user.</summary>
    [HttpPost("block/{userId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<bool>>> BlockUser(Guid userId)
    {
        await _friendService.BlockUserAsync(User.GetUserId(), userId);
        return Ok(ApiResponse<bool>.Ok(true));
    }

    /// <summary>Unblocks a user.</summary>
    [HttpDelete("block/{userId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<bool>>> UnblockUser(Guid userId)
    {
        await _friendService.UnblockUserAsync(User.GetUserId(), userId);
        return Ok(ApiResponse<bool>.Ok(true));
    }

    /// <summary>Gets blocked users.</summary>
    [HttpGet("blocked")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyCollection<BlockedUserDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<BlockedUserDto>>>> GetBlockedUsers()
    {
        var users = await _friendService.GetBlockedUsersAsync(User.GetUserId());
        return Ok(ApiResponse<IReadOnlyCollection<BlockedUserDto>>.Ok(users));
    }

    /// <summary>Request body for sending a friend request.</summary>
    public record SendFriendRequestBody(Guid ToUserId, string Message);
}
