#nullable enable

using Microsoft.Extensions.Logging;
using SecureChat.Application.Services.Realtime;
using SecureChat.Domain.Exceptions;
using SecureChat.Domain.Interfaces.Repositories;
using SecureChat.Shared.DTOs.Friend;
using SecureChat.Shared.DTOs.User;
using SecureChat.Shared.Enums;
using DomainBlockedUser = SecureChat.Domain.Entities.BlockedUser;
using DomainFriend = SecureChat.Domain.Entities.Friend;
using DomainFriendRequest = SecureChat.Domain.Entities.FriendRequest;

namespace SecureChat.Application.Services.Friend;

/// <summary>
/// Provides friend management workflows.
/// </summary>
public class FriendService : IFriendService
{
    private static readonly TimeSpan OnlineWindow = TimeSpan.FromSeconds(45);

    private readonly IUserRepository _userRepository;
    private readonly IFriendRepository _friendRepository;
    private readonly IFriendRequestRepository _friendRequestRepository;
    private readonly IRepository<DomainBlockedUser> _blockedUserRepository;
    private readonly IRealtimeNotifier _realtimeNotifier;
    private readonly ILogger<FriendService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="FriendService"/> class.
    /// </summary>
    public FriendService(
        IUserRepository userRepository,
        IFriendRepository friendRepository,
        IFriendRequestRepository friendRequestRepository,
        IRepository<DomainBlockedUser> blockedUserRepository,
        IRealtimeNotifier realtimeNotifier,
        ILogger<FriendService> logger)
    {
        _userRepository = userRepository;
        _friendRepository = friendRepository;
        _friendRequestRepository = friendRequestRepository;
        _blockedUserRepository = blockedUserRepository;
        _realtimeNotifier = realtimeNotifier;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<FriendRequestDto> SendFriendRequestAsync(Guid fromUserId, Guid toUserId, string message)
    {
        _logger.LogInformation("Sending friend request from {FromUserId} to {ToUserId}.", fromUserId, toUserId);

        if (fromUserId == toUserId)
        {
            throw new DomainException("A user cannot send a friend request to themselves.", 400);
        }

        var fromUser = await _userRepository.GetByIdAsync(fromUserId) ?? throw new UserNotFoundException(fromUserId);
        var toUser = await _userRepository.GetByIdAsync(toUserId) ?? throw new UserNotFoundException(toUserId);

        if (await _friendRepository.AreFriendsAsync(fromUserId, toUserId))
        {
            throw new DomainException("Users are already friends.", 409);
        }

        var blockedUsers = await _blockedUserRepository.GetAllAsync();
        if (blockedUsers.Any(block => (block.UserId == fromUserId && block.BlockedUserId == toUserId) || (block.UserId == toUserId && block.BlockedUserId == fromUserId)))
        {
            throw new DomainException("Friend request cannot be sent because one user has blocked the other.", 403);
        }

        var existingRequest = await _friendRequestRepository.GetBetweenUsersAsync(fromUserId, toUserId);
        if (existingRequest is not null && existingRequest.Status == FriendStatus.Pending)
        {
            throw new DomainException("A pending friend request already exists.", 409);
        }

        var request = new DomainFriendRequest(fromUserId, toUserId, message);
        await _friendRequestRepository.AddAsync(request);
        request.FromUser = fromUser;
        request.ToUser = toUser;

        var dto = MapFriendRequest(request);
        await _realtimeNotifier.SendFriendRequestAsync(toUserId, dto);
        return dto;
    }

    /// <inheritdoc />
    public async Task<bool> RespondToRequestAsync(Guid requestId, Guid userId, bool accept)
    {
        _logger.LogInformation("Responding to friend request {RequestId} by user {UserId}.", requestId, userId);

        var request = await _friendRequestRepository.GetByIdAsync(requestId)
            ?? throw new DomainException($"Friend request '{requestId}' was not found.", 404);

        if (request.ToUserId != userId)
        {
            throw new UnauthorizedException("Only the request recipient can respond.");
        }

        if (request.Status != FriendStatus.Pending)
        {
            throw new DomainException("Friend request has already been answered.", 409);
        }

        request.Status = accept ? FriendStatus.Friends : FriendStatus.None;
        request.RespondedAt = DateTimeOffset.UtcNow;
        await _friendRequestRepository.UpdateAsync(request);

        if (accept)
        {
            await _friendRepository.AddAsync(new DomainFriend(request.FromUserId, request.ToUserId));
            await _friendRepository.AddAsync(new DomainFriend(request.ToUserId, request.FromUserId));
        }

        await _realtimeNotifier.SendFriendRequestResponseAsync(request.FromUserId, request.Id, accept, userId);
        await _realtimeNotifier.SendFriendRequestResponseAsync(request.ToUserId, request.Id, accept, userId);
        return accept;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<FriendDto>> GetFriendsAsync(Guid userId)
    {
        var friendships = await _friendRepository.GetFriendsAsync(userId);
        return friendships
            .Where(friend => friend.User is not null && friend.FriendUser is not null)
            .Select(friend =>
            {
                var friendUser = friend.UserId == userId ? friend.FriendUser : friend.User;
                return new FriendDto
                {
                    UserId = friendUser.Id,
                    DisplayName = friendUser.DisplayName,
                    AvatarUrl = friendUser.AvatarUrl,
                    IsOnline = IsCurrentlyOnline(friendUser),
                    FriendshipId = friend.Id
                };
            })
            .Where(friend => friend.UserId != userId && friend.UserId != Guid.Empty)
            .GroupBy(friend => friend.UserId)
            .Select(group => group.First())
            .ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<FriendRequestDto>> GetPendingRequestsAsync(Guid userId)
    {
        var requests = await _friendRequestRepository.GetPendingForUserAsync(userId);
        return requests.Select(MapFriendRequest).ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<FriendRequestDto>> GetSentPendingRequestsAsync(Guid userId)
    {
        var requests = await _friendRequestRepository.GetPendingSentByUserAsync(userId);
        return requests.Select(MapFriendRequest).ToList();
    }

    /// <inheritdoc />
    public async Task RemoveFriendAsync(Guid userId, Guid friendId)
    {
        await _friendRepository.RemoveFriendAsync(userId, friendId);
    }

    /// <inheritdoc />
    public async Task BlockUserAsync(Guid userId, Guid targetId)
    {
        _logger.LogInformation("Blocking user {TargetId} for user {UserId}.", targetId, userId);

        _ = await _userRepository.GetByIdAsync(targetId) ?? throw new UserNotFoundException(targetId);
        await _friendRepository.RemoveFriendAsync(userId, targetId);
        var request = await _friendRequestRepository.GetBetweenUsersAsync(userId, targetId);
        if (request is not null && request.Status == FriendStatus.Pending)
        {
            request.Status = FriendStatus.None;
            request.RespondedAt = DateTimeOffset.UtcNow;
            await _friendRequestRepository.UpdateAsync(request);
        }

        var existingBlocks = await _blockedUserRepository.GetAllAsync();
        if (existingBlocks.Any(block => block.UserId == userId && block.BlockedUserId == targetId))
        {
            return;
        }

        await _blockedUserRepository.AddAsync(new DomainBlockedUser(userId, targetId));
    }

    /// <inheritdoc />
    public async Task UnblockUserAsync(Guid userId, Guid targetId)
    {
        _logger.LogInformation("Unblocking user {TargetId} for user {UserId}.", targetId, userId);

        var blocks = await _blockedUserRepository.GetAllAsync();
        var block = blocks.FirstOrDefault(item => item.UserId == userId && item.BlockedUserId == targetId);
        if (block is null)
        {
            return;
        }

        await _blockedUserRepository.DeleteAsync(block.Id);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<BlockedUserDto>> GetBlockedUsersAsync(Guid userId)
    {
        var blocks = await _blockedUserRepository.GetAllAsync();
        var users = new List<BlockedUserDto>();

        foreach (var block in blocks.Where(block => block.UserId == userId))
        {
            var blockedUser = await _userRepository.GetByIdAsync(block.BlockedUserId);
            if (blockedUser is null)
            {
                continue;
            }

            users.Add(new BlockedUserDto
            {
                UserId = blockedUser.Id,
                DisplayName = blockedUser.DisplayName,
                BlockedAt = block.BlockedAt
            });
        }

        return users;
    }

    private static FriendRequestDto MapFriendRequest(DomainFriendRequest request)
    {
        var fromUser = request.FromUser ?? new SecureChat.Domain.Entities.User { Id = request.FromUserId, DisplayName = "Unknown user" };
        var toUser = request.ToUser ?? new SecureChat.Domain.Entities.User { Id = request.ToUserId, DisplayName = "Unknown user" };

        return new FriendRequestDto
        {
            Id = request.Id,
            FromUser = new UserDto
            {
                Id = fromUser.Id,
                DisplayName = fromUser.DisplayName,
                Email = fromUser.Email,
                AvatarUrl = fromUser.AvatarUrl,
                IsOnline = fromUser.IsOnline,
                LastSeen = fromUser.LastSeen
            },
            ToUser = new UserDto
            {
                Id = toUser.Id,
                DisplayName = toUser.DisplayName,
                Email = toUser.Email,
                AvatarUrl = toUser.AvatarUrl,
                IsOnline = toUser.IsOnline,
                LastSeen = toUser.LastSeen
            },
            ToUserId = request.ToUserId,
            Message = request.Message,
            SentAt = request.SentAt,
            Status = request.Status
        };
    }

    private static bool IsCurrentlyOnline(SecureChat.Domain.Entities.User user)
    {
        return user.IsOnline && user.LastSeen is not null && DateTimeOffset.UtcNow - user.LastSeen.Value <= OnlineWindow;
    }
}
