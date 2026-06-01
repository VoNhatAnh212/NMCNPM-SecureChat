#nullable enable

using SecureChat.Shared.DTOs.Group;

namespace SecureChat.Client.Services.Api;

/// <summary>
/// Provides group API operations.
/// </summary>
public class GroupApiService
{
    private readonly IApiClient _apiClient;

    /// <summary>Initializes a new instance of the <see cref="GroupApiService"/> class.</summary>
    public GroupApiService(IApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    /// <summary>Gets groups.</summary>
    public async Task<List<GroupDto>> GetGroupsAsync() => (await _apiClient.GetAsync<IReadOnlyCollection<GroupDto>>("/api/groups")).ToList();

    /// <summary>Creates a group.</summary>
    public Task<GroupDto> CreateGroupAsync(CreateGroupDto dto) => _apiClient.PostAsync<CreateGroupDto, GroupDto>("/api/groups", dto);

    /// <summary>Gets group details.</summary>
    public Task<GroupDto> GetGroupDetailsAsync(Guid groupId) => _apiClient.GetAsync<GroupDto>($"/api/groups/{groupId}");

    /// <summary>Updates group metadata.</summary>
    public Task<GroupDto> UpdateGroupAsync(Guid groupId, UpdateGroupDto dto) => _apiClient.PutAsync<UpdateGroupDto, GroupDto>($"/api/groups/{groupId}", dto);

    /// <summary>Adds a group member.</summary>
    public async Task AddMemberAsync(Guid groupId, Guid userId) => await _apiClient.PostAsync<object, bool>($"/api/groups/{groupId}/members", new { userId });

    /// <summary>Removes a group member.</summary>
    public async Task RemoveMemberAsync(Guid groupId, Guid userId) => await _apiClient.DeleteAsync($"/api/groups/{groupId}/members/{userId}");

    /// <summary>Leaves a group as the current user.</summary>
    public async Task LeaveGroupAsync(Guid groupId) => await _apiClient.PostAsync<object, bool>($"/api/groups/{groupId}/leave", new { });

    /// <summary>Uploads group keys.</summary>
    public async Task UploadGroupKeysAsync(Guid groupId, List<GroupKeyDto> keys) => await _apiClient.PostAsync<List<GroupKeyDto>, bool>($"/api/groups/{groupId}/keys", keys);

    /// <summary>Gets group keys for a device.</summary>
    public async Task<List<GroupKeyDto>> GetGroupKeysAsync(Guid groupId, string deviceId) => (await _apiClient.GetAsync<IReadOnlyCollection<GroupKeyDto>>($"/api/groups/{groupId}/keys/{deviceId}")).ToList();
}
