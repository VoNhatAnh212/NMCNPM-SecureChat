#nullable enable

namespace SecureChat.Client.Services.Api;

/// <summary>
/// Reserved message API service for future message-specific endpoints.
/// </summary>
public class MessageApiService
{
    /// <summary>Initializes a new instance of the <see cref="MessageApiService"/> class.</summary>
    public MessageApiService(ChatApiService chatApiService)
    {
        ChatApiService = chatApiService;
    }

    /// <summary>Gets the chat API service that currently owns message endpoints.</summary>
    public ChatApiService ChatApiService { get; }
}
