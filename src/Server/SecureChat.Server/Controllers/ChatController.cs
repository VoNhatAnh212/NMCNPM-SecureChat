#nullable enable

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Mvc;
using SecureChat.Application.Services.Message;
using SecureChat.Server.Extensions;
using SecureChat.Server.Hubs;
using SecureChat.Server.Responses;
using SecureChat.Shared.Constants;
using SecureChat.Shared.DTOs.Chat;
using SecureChat.Shared.DTOs.Message;

namespace SecureChat.Server.Controllers;

/// <summary>
/// Provides chat API endpoints.
/// </summary>
[Authorize]
[ApiController]
[Route("api/chats")]
public class ChatController : ControllerBase
{
    private readonly IMessageService _messageService;
    private readonly IHubContext<ChatHub> _hubContext;

    /// <summary>Initializes a new instance of the <see cref="ChatController"/> class.</summary>
    public ChatController(IMessageService messageService, IHubContext<ChatHub> hubContext)
    {
        _messageService = messageService;
        _hubContext = hubContext;
    }

    /// <summary>Gets the current user's chat list.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyCollection<ChatSummaryDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<ChatSummaryDto>>>> GetChats()
    {
        var chats = await _messageService.GetChatListAsync(User.GetUserId());
        return Ok(ApiResponse<IReadOnlyCollection<ChatSummaryDto>>.Ok(chats));
    }

    /// <summary>Gets or creates a personal chat.</summary>
    [HttpGet("{chatId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<ChatDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<ChatDto>>> GetChat(Guid chatId)
    {
        var chat = await _messageService.GetChatAsync(chatId, User.GetUserId());
        return Ok(ApiResponse<ChatDto>.Ok(chat));
    }

    /// <summary>Gets paged encrypted chat messages.</summary>
    [HttpGet("{chatId:guid}/messages")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyCollection<MessageDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<MessageDto>>>> GetMessages(Guid chatId, [FromQuery] int page = 1, [FromQuery] int size = 50)
    {
        var messages = await _messageService.GetChatMessagesAsync(chatId, User.GetUserId(), page, size);
        return Ok(ApiResponse<IReadOnlyCollection<MessageDto>>.Ok(messages));
    }

    /// <summary>Sends an encrypted message through HTTP.</summary>
    [HttpPost("{chatId:guid}/messages")]
    [ProducesResponseType(typeof(ApiResponse<MessageDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<MessageDto>>> SendMessage(Guid chatId, [FromBody] SendMessageDto dto)
    {
        var senderId = User.GetUserId();
        var message = await _messageService.SendMessageAsync(senderId, dto with { ChatId = chatId });
        var recipientIds = await _messageService.GetChatParticipantIdsAsync(chatId);

        foreach (var recipientId in recipientIds.Where(id => id != senderId))
        {
            await _hubContext.Clients.Group($"user:{recipientId}").SendAsync(SignalREvents.ReceiveMessage, message);
        }

        return Ok(ApiResponse<MessageDto>.Ok(message));
    }

    /// <summary>Gets or creates a personal chat.</summary>
    [HttpPost("personal/{otherUserId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<ChatDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<ChatDto>>> GetOrCreatePersonalChat(Guid otherUserId)
    {
        var chat = await _messageService.GetOrCreatePersonalChatAsync(User.GetUserId(), otherUserId);
        return Ok(ApiResponse<ChatDto>.Ok(chat));
    }

    /// <summary>Stores an encrypted session key for a chat recipient device.</summary>
    [HttpPost("{chatId:guid}/session-key")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<bool>>> ExchangeSessionKey(Guid chatId, [FromBody] SessionKeyDto dto)
    {
        await _messageService.ExchangeSessionKeyAsync(chatId, User.GetUserId(), dto);
        return Ok(ApiResponse<bool>.Ok(true));
    }

    /// <summary>Gets an encrypted session key for a device.</summary>
    [HttpGet("{chatId:guid}/session-key/{deviceId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<SessionKeyDto?>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<SessionKeyDto?>>> GetSessionKey(Guid chatId, Guid deviceId)
    {
        var sessionKey = await _messageService.GetSessionKeyAsync(chatId, deviceId, User.GetUserId());
        return Ok(ApiResponse<SessionKeyDto?>.Ok(sessionKey));
    }

    /// <summary>Removes a chat from the current user's chat list.</summary>
    [HttpDelete("{chatId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteChat(Guid chatId)
    {
        await _messageService.DeleteChatForUserAsync(chatId, User.GetUserId());
        return Ok(ApiResponse<bool>.Ok(true));
    }
}
