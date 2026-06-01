#nullable enable

using System.Configuration;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR.Client;
using SecureChat.Client.Services.Api;
using SecureChat.Shared.Constants;
using SecureChat.Shared.DTOs.Friend;
using SecureChat.Shared.DTOs.Message;

namespace SecureChat.Client.Services.SignalR;

/// <summary>
/// Wraps the SecureChat SignalR hub connection.
/// </summary>
public class SignalRService : ISignalRService
{
    private HubConnection? _connection;

    /// <inheritdoc />
    public event EventHandler<MessageDto>? MessageReceived;

    /// <inheritdoc />
    public event EventHandler<TypingEventArgs>? TypingReceived;

    /// <inheritdoc />
    public event EventHandler<OnlineStatusEventArgs>? OnlineStatusChanged;

    /// <inheritdoc />
    public event EventHandler<FriendRequestDto>? FriendRequestReceived;

    /// <inheritdoc />
    public event EventHandler<string>? FriendRequestResponded;

    /// <inheritdoc />
    public event EventHandler<GroupUpdateEventArgs>? GroupUpdated;

    /// <inheritdoc />
    public event EventHandler<string>? DeviceRevoked;

    /// <inheritdoc />
    public bool IsConnected => _connection?.State == HubConnectionState.Connected;

    /// <inheritdoc />
    public async Task ConnectAsync(string accessToken)
    {
        if (_connection is not null)
        {
            await DisconnectAsync();
        }

        var hubUrl = ConfigurationManager.AppSettings["SignalRHubUrl"] ?? "https://localhost:5001/hubs/chat";
        _connection = new HubConnectionBuilder()
            .WithUrl($"{hubUrl}?access_token={Uri.EscapeDataString(accessToken)}", options =>
            {
                options.HttpMessageHandlerFactory = _ => ServerCertificateValidator.CreateHttpClientHandler();
            })
            .WithAutomaticReconnect()
            .Build();

        RegisterHandlers(_connection);
        await _connection.StartAsync();
    }

    /// <inheritdoc />
    public async Task DisconnectAsync()
    {
        if (_connection is null)
        {
            return;
        }

        await _connection.StopAsync();
        await _connection.DisposeAsync();
        _connection = null;
    }

    /// <inheritdoc />
    public async Task SendMessageAsync(SendMessageDto dto)
    {
        EnsureConnected();
        await _connection!.InvokeAsync("SendMessageAsync", dto);
    }

    /// <inheritdoc />
    public async Task SendTypingAsync(Guid chatId, bool isTyping)
    {
        EnsureConnected();
        await _connection!.SendAsync("SendTypingAsync", chatId, isTyping);
    }

    private void RegisterHandlers(HubConnection connection)
    {
        connection.On<MessageDto>(SignalREvents.ReceiveMessage, message => MessageReceived?.Invoke(this, message));
        connection.On<TypingEventArgs>(SignalREvents.Typing, args => TypingReceived?.Invoke(this, args));
        connection.On<OnlineStatusEventArgs>(SignalREvents.OnlineStatus, args => OnlineStatusChanged?.Invoke(this, args));
        connection.On<FriendRequestDto>(SignalREvents.FriendRequest, request => FriendRequestReceived?.Invoke(this, request));
        connection.On<JsonElement>(SignalREvents.FriendRequestResponse, payload => FriendRequestResponded?.Invoke(this, payload.GetRawText()));
        connection.On<JsonElement>(SignalREvents.GroupUpdate, payload =>
        {
            if (!TryReadGuid(payload, "groupId", out var groupId))
            {
                return;
            }

            var eventType = TryReadString(payload, "eventType") ?? string.Empty;
            var data = payload.TryGetProperty("data", out var dataElement) ? dataElement.GetRawText() : null;
            GroupUpdated?.Invoke(this, new GroupUpdateEventArgs(groupId, eventType, data));
        });
        connection.On<JsonElement>(SignalREvents.DeviceUpdate, payload => DeviceRevoked?.Invoke(this, payload.GetRawText()));
    }

    private static bool TryReadGuid(JsonElement payload, string propertyName, out Guid value)
    {
        value = Guid.Empty;
        if (!payload.TryGetProperty(propertyName, out var element))
        {
            return false;
        }

        return element.ValueKind == JsonValueKind.String && Guid.TryParse(element.GetString(), out value);
    }

    private static string? TryReadString(JsonElement payload, string propertyName)
    {
        return payload.TryGetProperty(propertyName, out var element) && element.ValueKind == JsonValueKind.String
            ? element.GetString()
            : null;
    }

    private void EnsureConnected()
    {
        if (!IsConnected)
        {
            throw new InvalidOperationException("SignalR connection is not active.");
        }
    }
}

/// <summary>
/// Represents a typing realtime event.
/// </summary>
public record TypingEventArgs(Guid ChatId, Guid UserId, string DisplayName, bool IsTyping);

/// <summary>
/// Represents an online status realtime event.
/// </summary>
public record OnlineStatusEventArgs(Guid UserId, bool IsOnline, DateTime LastSeen);

/// <summary>
/// Represents a group update realtime event.
/// </summary>
public record GroupUpdateEventArgs(Guid GroupId, string EventType, string? Data);
