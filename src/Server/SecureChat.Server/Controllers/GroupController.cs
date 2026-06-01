#nullable enable

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecureChat.Application.Services.Group;
using SecureChat.Server.Extensions;
using SecureChat.Server.Responses;
using SecureChat.Shared.DTOs.Group;

namespace SecureChat.Server.Controllers;

/// <summary>
/// Provides group API endpoints.
/// </summary>
[Authorize]
[ApiController]
[Route("api/groups")]
public class GroupController : ControllerBase
{
    private readonly IGroupService _groupService;

    /// <summary>Initializes a new instance of the <see cref="GroupController"/> class.</summary>
    public GroupController(IGroupService groupService)
    {
        _groupService = groupService;
    }

    /// <summary>Gets groups for the current user.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyCollection<GroupDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<GroupDto>>>> GetGroups()
    {
        var groups = await _groupService.GetUserGroupsAsync(User.GetUserId());
        return Ok(ApiResponse<IReadOnlyCollection<GroupDto>>.Ok(groups));
    }

    /// <summary>Creates a group.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<GroupDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<GroupDto>>> CreateGroup([FromBody] CreateGroupDto dto)
    {
        var group = await _groupService.CreateGroupAsync(User.GetUserId(), dto);
        return Ok(ApiResponse<GroupDto>.Ok(group));
    }

    /// <summary>Gets group details.</summary>
    [HttpGet("{groupId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<GroupDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<GroupDto>>> GetGroup(Guid groupId)
    {
        var group = await _groupService.GetGroupDetailsAsync(groupId, User.GetUserId());
        return Ok(ApiResponse<GroupDto>.Ok(group));
    }

    /// <summary>Updates group metadata.</summary>
    [HttpPut("{groupId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<GroupDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<GroupDto>>> UpdateGroup(Guid groupId, [FromBody] UpdateGroupDto dto)
    {
        var group = await _groupService.UpdateGroupAsync(groupId, User.GetUserId(), dto);
        return Ok(ApiResponse<GroupDto>.Ok(group));
    }

    /// <summary>Dissolves a group.</summary>
    [HttpDelete("{groupId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<bool>>> DissolveGroup(Guid groupId)
    {
        await _groupService.DissolveGroupAsync(groupId, User.GetUserId());
        return Ok(ApiResponse<bool>.Ok(true));
    }

    /// <summary>Adds a group member.</summary>
    [HttpPost("{groupId:guid}/members")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<bool>>> AddMember(Guid groupId, [FromBody] AddMemberBody body)
    {
        await _groupService.AddMemberAsync(groupId, User.GetUserId(), body.UserId);
        return Ok(ApiResponse<bool>.Ok(true));
    }

    /// <summary>Removes a group member.</summary>
    [HttpDelete("{groupId:guid}/members/{userId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<bool>>> RemoveMember(Guid groupId, Guid userId)
    {
        await _groupService.RemoveMemberAsync(groupId, User.GetUserId(), userId);
        return Ok(ApiResponse<bool>.Ok(true));
    }

    /// <summary>Leaves a group as the current user.</summary>
    [HttpPost("{groupId:guid}/leave")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<bool>>> LeaveGroup(Guid groupId)
    {
        var userId = User.GetUserId();
        await _groupService.RemoveMemberAsync(groupId, userId, userId);
        return Ok(ApiResponse<bool>.Ok(true));
    }

    /// <summary>Uploads encrypted group keys.</summary>
    [HttpPost("{groupId:guid}/keys")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<bool>>> UploadGroupKeys(Guid groupId, [FromBody] List<GroupKeyDto> keys)
    {
        await _groupService.UploadGroupKeysAsync(groupId, keys);
        return Ok(ApiResponse<bool>.Ok(true));
    }

    /// <summary>Gets encrypted group keys for a device.</summary>
    [HttpGet("{groupId:guid}/keys/{deviceId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyCollection<GroupKeyDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<GroupKeyDto>>>> GetGroupKeys(Guid groupId, Guid deviceId)
    {
        var keys = await _groupService.GetGroupKeysAsync(groupId, deviceId);
        return Ok(ApiResponse<IReadOnlyCollection<GroupKeyDto>>.Ok(keys));
    }

    /// <summary>Uploads a group avatar.</summary>
    [HttpPut("{groupId:guid}/avatar")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ApiResponse<GroupDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<GroupDto>>> UploadGroupAvatar(Guid groupId, IFormFile file)
    {
        await using var stream = file.OpenReadStream();
        var group = await _groupService.UploadGroupAvatarAsync(groupId, User.GetUserId(), stream, file.FileName);
        return Ok(ApiResponse<GroupDto>.Ok(group));
    }

    /// <summary>Request body for adding a member.</summary>
    public record AddMemberBody(Guid UserId);
}
