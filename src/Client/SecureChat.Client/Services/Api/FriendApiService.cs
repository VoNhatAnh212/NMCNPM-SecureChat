#nullable enable

using SecureChat.Shared.DTOs.Friend;
using SecureChat.Shared.DTOs.User;

namespace SecureChat.Client.Services.Api;

/// <summary>
/// Provides friend API operations.
/// </summary>
public class FriendApiService
{
    private readonly IApiClient _apiClient;

    /// <summary>Initializes a new instance of the <see cref="FriendApiService"/> class.</summary>
    public FriendApiService(IApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    /// <summary>Gets friends.</summary>
    public async Task<List<FriendDto>> GetFriendsAsync() => (await _apiClient.GetAsync<IReadOnlyCollection<FriendDto>>("/api/friends")).ToList();

    /// <summary>Gets friend requests.</summary>
    public async Task<List<FriendRequestDto>> GetFriendRequestsAsync() => (await _apiClient.GetAsync<IReadOnlyCollection<FriendRequestDto>>("/api/friends/requests")).ToList();

    /// <summary>Gets sent pending friend requests.</summary>
    public async Task<List<FriendRequestDto>> GetSentFriendRequestsAsync() => (await _apiClient.GetAsync<IReadOnlyCollection<FriendRequestDto>>("/api/friends/requests/sent")).ToList();

    /// <summary>Sends a friend request.</summary>
    public Task<FriendRequestDto> SendFriendRequestAsync(Guid toUserId, string message) => _apiClient.PostAsync<object, FriendRequestDto>("/api/friends/requests", new { toUserId, message });

    /// <summary>Accepts a friend request.</summary>
    public Task<bool> AcceptRequestAsync(Guid requestId) => _apiClient.PutAsync<object, bool>($"/api/friends/requests/{requestId}/accept", new { });

    /// <summary>Rejects a friend request.</summary>
    public Task<bool> RejectRequestAsync(Guid requestId) => _apiClient.PutAsync<object, bool>($"/api/friends/requests/{requestId}/reject", new { });

    /// <summary>Removes a friend.</summary>
    public Task<bool> RemoveFriendAsync(Guid friendId) => _apiClient.DeleteAsync($"/api/friends/{friendId}");

    /// <summary>Blocks a user.</summary>
    public Task<bool> BlockUserAsync(Guid userId) => _apiClient.PostAsync<object, bool>($"/api/friends/block/{userId}", new { });

    /// <summary>Unblocks a user.</summary>
    public Task<bool> UnblockUserAsync(Guid userId) => _apiClient.DeleteAsync($"/api/friends/block/{userId}");

    /// <summary>Gets blocked users.</summary>
    public async Task<List<BlockedUserDto>> GetBlockedUsersAsync() => (await _apiClient.GetAsync<IReadOnlyCollection<BlockedUserDto>>("/api/friends/blocked")).ToList();

    /// <summary>Searches users.</summary>
    public async Task<List<UserDto>> SearchUsersAsync(string query) => (await _apiClient.GetAsync<IReadOnlyCollection<UserDto>>($"/api/users/search?q={Uri.EscapeDataString(query)}")).ToList();
}
