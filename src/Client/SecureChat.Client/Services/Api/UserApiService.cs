#nullable enable

using SecureChat.Shared.DTOs.User;

namespace SecureChat.Client.Services.Api;

/// <summary>
/// Provides user lookup and public-key API operations.
/// </summary>
public class UserApiService
{
    private readonly IApiClient _apiClient;

    /// <summary>Initializes a new instance of the <see cref="UserApiService"/> class.</summary>
    public UserApiService(IApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    /// <summary>Searches users by query.</summary>
    public async Task<List<UserDto>> SearchUsersAsync(string query) =>
        (await _apiClient.GetAsync<IReadOnlyCollection<UserDto>>($"/api/users/search?q={Uri.EscapeDataString(query)}")).ToList();

    /// <summary>Gets active public keys for a user.</summary>
    public async Task<List<PublicKeyDto>> GetPublicKeysAsync(Guid userId) =>
        (await _apiClient.GetAsync<IReadOnlyCollection<PublicKeyDto>>($"/api/users/{userId}/public-keys")).ToList();

    /// <summary>Updates the current user's presence heartbeat.</summary>
    public Task<bool> PingPresenceAsync() => _apiClient.PostAsync<object, bool>("/api/users/presence/ping", new { });
}
