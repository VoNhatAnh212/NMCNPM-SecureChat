#nullable enable

using Microsoft.Extensions.Logging;
using SecureChat.Application.Services.Realtime;
using SecureChat.Domain.Exceptions;
using SecureChat.Domain.Interfaces.Repositories;
using SecureChat.Shared.DTOs.Group;
using SecureChat.Shared.Enums;
using DomainGroup = SecureChat.Domain.Entities.Group;
using DomainChat = SecureChat.Domain.Entities.Chat;
using DomainGroupKey = SecureChat.Domain.Entities.GroupKey;
using DomainGroupMember = SecureChat.Domain.Entities.GroupMember;

namespace SecureChat.Application.Services.Group;

/// <summary>
/// Provides group chat workflows.
/// </summary>
public class GroupService : IGroupService
{
    private readonly IGroupRepository _groupRepository;
    private readonly IChatRepository _chatRepository;
    private readonly IRepository<DomainGroupMember> _groupMemberRepository;
    private readonly IRepository<DomainGroupKey> _groupKeyRepository;
    private readonly IUserRepository _userRepository;
    private readonly IRepository<SecureChat.Domain.Entities.BlockedUser> _blockedUserRepository;
    private readonly IRealtimeNotifier _realtimeNotifier;
    private readonly ILogger<GroupService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="GroupService"/> class.
    /// </summary>
    public GroupService(
        IGroupRepository groupRepository,
        IChatRepository chatRepository,
        IRepository<DomainGroupMember> groupMemberRepository,
        IRepository<DomainGroupKey> groupKeyRepository,
        IUserRepository userRepository,
        IRepository<SecureChat.Domain.Entities.BlockedUser> blockedUserRepository,
        IRealtimeNotifier realtimeNotifier,
        ILogger<GroupService> logger)
    {
        _groupRepository = groupRepository;
        _chatRepository = chatRepository;
        _groupMemberRepository = groupMemberRepository;
        _groupKeyRepository = groupKeyRepository;
        _userRepository = userRepository;
        _blockedUserRepository = blockedUserRepository;
        _realtimeNotifier = realtimeNotifier;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<GroupDto>> GetUserGroupsAsync(Guid userId)
    {
        var groups = await _groupRepository.GetByMemberIdAsync(userId);
        return groups.Select(MapGroup).ToList();
    }

    /// <inheritdoc />
    public async Task<GroupDto> CreateGroupAsync(Guid creatorId, CreateGroupDto dto)
    {
        _logger.LogInformation("Creating group {GroupName} by user {CreatorId}.", dto.Name, creatorId);

        _ = await _userRepository.GetByIdAsync(creatorId) ?? throw new UserNotFoundException(creatorId);

        var group = new DomainGroup(dto.Name, creatorId);
        group.Members.Add(new DomainGroupMember(group.Id, creatorId, GroupRole.Owner));

        foreach (var memberId in dto.MemberIds.Distinct().Where(memberId => memberId != creatorId))
        {
            _ = await _userRepository.GetByIdAsync(memberId) ?? throw new UserNotFoundException(memberId);
            await EnsureNotBlockedAsync(creatorId, memberId);
            group.Members.Add(new DomainGroupMember(group.Id, memberId, GroupRole.Member));
        }

        await _groupRepository.AddAsync(group);

        var chat = new DomainChat(ChatType.Group, group.Id);
        foreach (var member in group.Members)
        {
            var user = await _userRepository.GetByIdAsync(member.UserId);
            if (user is not null)
            {
                chat.Participants.Add(user);
            }
        }

        await _chatRepository.AddAsync(chat);

        foreach (var member in group.Members.Where(member => member.UserId != creatorId))
        {
            await _realtimeNotifier.SendGroupUpdateAsync(member.UserId, group.Id, "created", new { group.Id, group.Name });
        }

        return MapGroup(group);
    }

    /// <inheritdoc />
    public async Task<GroupDto> GetGroupDetailsAsync(Guid groupId, Guid requesterId)
    {
        var group = await GetGroupWithMembersAsync(groupId);
        EnsureMember(group, requesterId);
        return MapGroup(group);
    }

    /// <inheritdoc />
    public async Task<GroupDto> UpdateGroupAsync(Guid groupId, Guid requesterId, UpdateGroupDto dto)
    {
        var group = await GetGroupWithMembersAsync(groupId);
        EnsureAdminOrOwner(group, requesterId);

        group.Name = dto.Name;
        group.AvatarUrl = dto.AvatarUrl;
        group.UpdatedAt = DateTimeOffset.UtcNow;
        await _groupRepository.UpdateAsync(group);
        await NotifyGroupMembersAsync(group, "updated", new { groupId, group.Name, group.AvatarUrl });

        return MapGroup(group);
    }

    /// <inheritdoc />
    public async Task DissolveGroupAsync(Guid groupId, Guid requesterId)
    {
        var group = await GetGroupWithMembersAsync(groupId);
        var requester = group.Members.FirstOrDefault(member => member.UserId == requesterId)
            ?? throw new UnauthorizedException("Requester is not a group member.");

        if (requester.Role != GroupRole.Owner)
        {
            throw new UnauthorizedException("Only the group owner can dissolve the group.");
        }

        await DissolveGroupInternalAsync(group, "dissolved");
    }

    /// <inheritdoc />
    public async Task AddMemberAsync(Guid groupId, Guid requesterId, Guid newMemberId)
    {
        var group = await GetGroupWithMembersAsync(groupId);
        EnsureMember(group, requesterId);

        if (group.Members.Any(member => member.UserId == newMemberId))
        {
            return;
        }

        _ = await _userRepository.GetByIdAsync(newMemberId) ?? throw new UserNotFoundException(newMemberId);
        foreach (var existingMember in group.Members)
        {
            await EnsureNotBlockedAsync(existingMember.UserId, newMemberId);
        }

        var member = new DomainGroupMember(groupId, newMemberId, GroupRole.Member);
        await _groupMemberRepository.AddAsync(member);
        await SyncGroupChatParticipantsAsync(groupId);
        var updatedGroup = await GetGroupWithMembersAsync(groupId);
        await NotifyGroupMembersAsync(updatedGroup, "memberAdded", new { groupId, userId = newMemberId, keyRotationRequired = true });
    }

    /// <inheritdoc />
    public async Task RemoveMemberAsync(Guid groupId, Guid requesterId, Guid targetId)
    {
        var group = await GetGroupWithMembersAsync(groupId);
        var requesterMembership = group.Members.FirstOrDefault(member => member.UserId == requesterId)
            ?? throw new UnauthorizedException("Requester is not a group member.");

        if (requesterId != targetId && requesterMembership.Role is not (GroupRole.Owner or GroupRole.Admin))
        {
            throw new UnauthorizedException("Only owners or admins can remove other members.");
        }

        var targetMembership = group.Members.FirstOrDefault(member => member.UserId == targetId)
            ?? throw new DomainException("Target user is not a group member.", 404);

        if (targetMembership.Role == GroupRole.Owner)
        {
            if (requesterId == targetId)
            {
                await DissolveGroupInternalAsync(group, "ownerLeft");
                return;
            }

            throw new UnauthorizedException("The group owner cannot be removed by another member.");
        }

        await _groupMemberRepository.DeleteAsync(targetMembership.Id);
        await SyncGroupChatParticipantsAsync(groupId);
        var updatedGroup = await GetGroupWithMembersAsync(groupId);
        EnsureActiveGroupHasOwner(updatedGroup);
        await NotifyGroupMembersAsync(updatedGroup, "memberRemoved", new { groupId, userId = targetId, keyRotationRequired = true });
        await _realtimeNotifier.SendGroupUpdateAsync(targetId, groupId, "memberRemoved", new { groupId, userId = targetId, keyRotationRequired = true });
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<GroupKeyDto>> GetGroupKeysAsync(Guid groupId, Guid deviceId)
    {
        var keys = await _groupKeyRepository.GetAllAsync();
        return keys
            .Where(key => key.GroupId == groupId && key.ForDeviceId == deviceId)
            .OrderByDescending(key => key.KeyVersion)
            .Select(key => new GroupKeyDto
            {
                GroupId = key.GroupId,
                EncryptedGroupKey = Convert.ToBase64String(key.EncryptedGroupKey),
                ForDeviceId = key.ForDeviceId,
                Version = key.KeyVersion
            })
            .ToList();
    }

    /// <inheritdoc />
    public async Task UploadGroupKeysAsync(Guid groupId, IReadOnlyCollection<GroupKeyDto> keys)
    {
        _ = await _groupRepository.GetByIdAsync(groupId) ?? throw new DomainException($"Group '{groupId}' was not found.", 404);

        foreach (var key in keys)
        {
            var groupKey = new DomainGroupKey(groupId, Convert.FromBase64String(key.EncryptedGroupKey), key.ForDeviceId, key.Version);
            await _groupKeyRepository.AddAsync(groupKey);
        }
    }

    /// <inheritdoc />
    public async Task<GroupDto> UploadGroupAvatarAsync(Guid groupId, Guid requesterId, Stream avatarStream, string fileName)
    {
        var group = await GetGroupWithMembersAsync(groupId);
        EnsureAdminOrOwner(group, requesterId);

        group.AvatarUrl = $"groups/{groupId:N}/avatar/{Path.GetFileName(fileName)}";
        group.UpdatedAt = DateTimeOffset.UtcNow;
        await _groupRepository.UpdateAsync(group);
        await NotifyGroupMembersAsync(group, "avatarUpdated", new { groupId, group.AvatarUrl });

        return MapGroup(group);
    }

    private async Task<DomainGroup> GetGroupWithMembersAsync(Guid groupId)
    {
        return await _groupRepository.GetWithMembersAsync(groupId)
            ?? throw new DomainException($"Group '{groupId}' was not found.", 404);
    }

    private static void EnsureAdminOrOwner(DomainGroup group, Guid requesterId)
    {
        var requesterMembership = group.Members.FirstOrDefault(member => member.UserId == requesterId)
            ?? throw new UnauthorizedException("Requester is not a group member.");

        if (requesterMembership.Role is not (GroupRole.Owner or GroupRole.Admin))
        {
            throw new UnauthorizedException("Only owners or admins can perform this action.");
        }
    }

    private static void EnsureMember(DomainGroup group, Guid userId)
    {
        if (!group.Members.Any(member => member.UserId == userId))
        {
            throw new UnauthorizedException("Requester is not a group member.");
        }
    }

    private async Task NotifyGroupMembersAsync(DomainGroup group, string eventType, object data)
    {
        foreach (var member in group.Members.GroupBy(member => member.UserId).Select(grouping => grouping.First()))
        {
            await _realtimeNotifier.SendGroupUpdateAsync(member.UserId, group.Id, eventType, data);
        }
    }

    private async Task DissolveGroupInternalAsync(DomainGroup group, string eventType)
    {
        var members = group.Members.ToList();
        var groupKeys = (await _groupKeyRepository.GetAllAsync()).Where(key => key.GroupId == group.Id).ToList();
        group.IsActive = false;
        group.UpdatedAt = DateTimeOffset.UtcNow;

        foreach (var member in members)
        {
            await _groupMemberRepository.DeleteAsync(member.Id);
        }

        foreach (var groupKey in groupKeys)
        {
            await _groupKeyRepository.DeleteAsync(groupKey.Id);
        }

        foreach (var chat in await _chatRepository.GetGroupChatsAsync(group.Id))
        {
            chat.Participants.Clear();
            await _chatRepository.UpdateAsync(chat);
        }

        group.Members.Clear();
        await _groupRepository.UpdateAsync(group);
        foreach (var member in members.GroupBy(member => member.UserId).Select(grouping => grouping.First()))
        {
            await _realtimeNotifier.SendGroupUpdateAsync(member.UserId, group.Id, eventType, new { groupId = group.Id, dissolved = true, keyRotationRequired = true });
        }
    }

    private static void EnsureActiveGroupHasOwner(DomainGroup group)
    {
        if (!group.Members.Any(member => member.Role == GroupRole.Owner))
        {
            throw new DomainException("An active group must have an owner.", 409);
        }
    }

    private async Task SyncGroupChatParticipantsAsync(Guid groupId)
    {
        var group = await GetGroupWithMembersAsync(groupId);
        var chats = (await _chatRepository.GetGroupChatsAsync(groupId)).ToList();
        if (chats.Count == 0)
        {
            var chat = new DomainChat(ChatType.Group, groupId);
            await _chatRepository.AddAsync(chat);
            chats.Add(chat);
        }

        foreach (var chat in chats)
        {
            chat.Participants.Clear();
            foreach (var member in group.Members)
            {
                var user = await _userRepository.GetByIdAsync(member.UserId);
                if (user is not null)
                {
                    chat.Participants.Add(user);
                }
            }

            await _chatRepository.UpdateAsync(chat);
        }
    }

    private async Task EnsureNotBlockedAsync(Guid userId, Guid otherUserId)
    {
        var blockedUsers = await _blockedUserRepository.GetAllAsync();
        if (blockedUsers.Any(block =>
                (block.UserId == userId && block.BlockedUserId == otherUserId)
                || (block.UserId == otherUserId && block.BlockedUserId == userId)))
        {
            throw new DomainException("Blocked users cannot be added to the same group.", 403);
        }
    }

    private static GroupDto MapGroup(DomainGroup group)
    {
        return new GroupDto
        {
            Id = group.Id,
            Name = group.Name,
            AvatarUrl = group.AvatarUrl,
            CreatorId = group.CreatorId,
            MemberCount = group.Members.Count,
            CreatedAt = group.CreatedAt,
            Members = group.Members.Select(member => new GroupMemberDto
            {
                UserId = member.UserId,
                DisplayName = member.User?.DisplayName ?? string.Empty,
                AvatarUrl = member.User?.AvatarUrl ?? string.Empty,
                Role = member.Role,
                JoinedAt = member.JoinedAt
            }).ToList()
        };
    }
}
