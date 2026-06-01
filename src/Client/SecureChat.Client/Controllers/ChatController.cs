#nullable enable

using SecureChat.Client.Services.Api;

namespace SecureChat.Client.Controllers;

/// <summary>
/// Coordinates chat UI workflows.
/// </summary>
public class ChatController
{
    /// <summary>Initializes a new instance of the <see cref="ChatController"/> class.</summary>
    public ChatController(ChatApiService chatApiService)
    {
        ChatApiService = chatApiService;
    }

    /// <summary>Gets the chat API service.</summary>
    public ChatApiService ChatApiService { get; }
}
