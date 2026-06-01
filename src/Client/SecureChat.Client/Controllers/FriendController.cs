#nullable enable

using SecureChat.Client.Services.Api;

namespace SecureChat.Client.Controllers;

/// <summary>
/// Coordinates friend UI workflows.
/// </summary>
public class FriendController
{
    /// <summary>Initializes a new instance of the <see cref="FriendController"/> class.</summary>
    public FriendController(FriendApiService friendApiService)
    {
        FriendApiService = friendApiService;
    }

    /// <summary>Gets the friend API service.</summary>
    public FriendApiService FriendApiService { get; }
}
