#nullable enable

using SecureChat.Client.Services.Api;

namespace SecureChat.Client.Controllers;

/// <summary>
/// Coordinates group UI workflows.
/// </summary>
public class GroupController
{
    /// <summary>Initializes a new instance of the <see cref="GroupController"/> class.</summary>
    public GroupController(GroupApiService groupApiService)
    {
        GroupApiService = groupApiService;
    }

    /// <summary>Gets the group API service.</summary>
    public GroupApiService GroupApiService { get; }
}
