#nullable enable

using SecureChat.Shared.DTOs.Group;

namespace SecureChat.Application.Services.Group;

/// <summary>
/// Defines group chat application operations.
/// </summary>
public interface IGroupService
{
    /// <summary>Gets groups for a user.</summary>
    Task<IReadOnlyCollection<GroupDto>> GetUserGroupsAsync(Guid userId);

    /// <summary>Creates a group.</summary>
    Task<GroupDto> CreateGroupAsync(Guid creatorId, CreateGroupDto dto);

    /// <summary>Gets group details.</summary>
    Task<GroupDto> GetGroupDetailsAsync(Guid groupId, Guid requesterId);

    /// <summary>Updates group metadata.</summary>
    Task<GroupDto> UpdateGroupAsync(Guid groupId, Guid requesterId, UpdateGroupDto dto);

    /// <summary>Dissolves a group.</summary>
    Task DissolveGroupAsync(Guid groupId, Guid requesterId);

    /// <summary>Adds a member to a group.</summary>
    Task AddMemberAsync(Guid groupId, Guid requesterId, Guid newMemberId);

    /// <summary>Removes a member from a group.</summary>
    Task RemoveMemberAsync(Guid groupId, Guid requesterId, Guid targetId);

    /// <summary>Gets group keys for a device.</summary>
    Task<IReadOnlyCollection<GroupKeyDto>> GetGroupKeysAsync(Guid groupId, Guid deviceId);

    /// <summary>Uploads encrypted group keys.</summary>
    Task UploadGroupKeysAsync(Guid groupId, IReadOnlyCollection<GroupKeyDto> keys);

    /// <summary>Uploads or updates a group avatar.</summary>
    Task<GroupDto> UploadGroupAvatarAsync(Guid groupId, Guid requesterId, Stream avatarStream, string fileName);
}
