#nullable enable

using Microsoft.Extensions.DependencyInjection;
using System.Text;
using System.Text.Json;
using SecureChat.Client.Controllers;
using SecureChat.Client.Forms.Auth;
using SecureChat.Client.Forms.Friends;
using SecureChat.Client.Forms.Groups;
using SecureChat.Client.Forms.Helpers;
using SecureChat.Client.Forms.Settings;
using SecureChat.Client.Forms.Main.Panels;
using SecureChat.Client.Services;
using SecureChat.Client.Services.Api;
using SecureChat.Client.Services.Crypto;
using SecureChat.Client.Services.SignalR;
using SecureChat.Client.Services.Storage;
using SecureChat.Shared.DTOs.Friend;
using SecureChat.Shared.DTOs.Message;

namespace SecureChat.Client.Forms.Main;

/// <summary>
/// Represents the main SecureChat shell.
/// </summary>
public partial class MainForm : Form
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ChatApiService _chatApiService;
    private readonly FriendApiService _friendApiService;
    private readonly UserApiService _userApiService;
    private readonly KeyStorageService _keyStorageService;
    private readonly AuthSessionService _authSession;
    private readonly ISignalRService _signalRService;
    private readonly AuthController _authController;
    private readonly SessionCache _sessionCache;
    private readonly System.Windows.Forms.Timer _presenceTimer = new() { Interval = 10000 };
    private readonly HashSet<Guid> _pendingGroupRefreshes = new();
    private Guid _currentUserId;
    private Guid? _activeChatId;
    private SecureChat.Shared.DTOs.Chat.ChatSummaryDto? _activeChat;
    private SecureChat.Shared.DTOs.Chat.ChatDto? _activeChatDetails;

    /// <summary>
    /// Initializes a new instance of the <see cref="MainForm"/> class.
    /// </summary>
    /// <param name="serviceProvider">The application service provider.</param>
    public MainForm(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _chatApiService = serviceProvider.GetRequiredService<ChatApiService>();
        _friendApiService = serviceProvider.GetRequiredService<FriendApiService>();
        _userApiService = serviceProvider.GetRequiredService<UserApiService>();
        _keyStorageService = serviceProvider.GetRequiredService<KeyStorageService>();
        _authSession = serviceProvider.GetRequiredService<AuthSessionService>();
        _signalRService = serviceProvider.GetRequiredService<ISignalRService>();
        _authController = serviceProvider.GetRequiredService<AuthController>();
        _sessionCache = serviceProvider.GetRequiredService<SessionCache>();
        InitializeComponent();
        _chatAreaPanel.MessageSent += ChatAreaPanelMessageSent;
        ThemeHelper.ApplyTheme(this);
        _presenceTimer.Tick += async (_, _) => await RefreshPresenceAsync();
    }

    /// <inheritdoc />
    protected override async void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        await InitializeAsync();
    }

    /// <inheritdoc />
    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _signalRService.MessageReceived -= OnMessageReceived;
        _signalRService.TypingReceived -= OnTypingReceived;
        _signalRService.OnlineStatusChanged -= OnOnlineStatusChanged;
        _signalRService.FriendRequestReceived -= OnFriendRequestReceived;
        _signalRService.FriendRequestResponded -= OnFriendRequestResponded;
        _signalRService.GroupUpdated -= OnGroupUpdated;
        _chatAreaPanel.MessageSent -= ChatAreaPanelMessageSent;
        _presenceTimer.Stop();
        base.OnFormClosed(e);
    }

    private async Task InitializeAsync()
    {
        var accounts = await _keyStorageService.LoadAccountsAsync();
        var tokens = _authSession.GetTokens();
        var currentUserId = ExtractGuidClaim(tokens.access, "sub");
        var currentDeviceId = ExtractStringClaim(tokens.access, "deviceId");
        var account = _authSession.CurrentAccount ?? accounts.FirstOrDefault(local =>
                (currentUserId is not null && local.Id == currentUserId)
                || (!string.IsNullOrWhiteSpace(currentDeviceId) && string.Equals(local.DeviceId, currentDeviceId, StringComparison.OrdinalIgnoreCase)))
            ?? accounts.OrderByDescending(local => local.LastLoginAt).FirstOrDefault();
        if (currentUserId is not null)
        {
            _currentUserId = currentUserId.Value;
            _leftSidebarPanel.SetCurrentUser(currentUserId.Value, account?.DisplayName ?? "SecureChat User");
            _chatAreaPanel.SetCurrentUser(currentUserId.Value);
        }
        else if (account is not null)
        {
            _currentUserId = account.Id;
            _leftSidebarPanel.SetCurrentUser(account.Id, account.DisplayName);
            _chatAreaPanel.SetCurrentUser(account.Id);
        }

        await LoadChatListAsync();
        await RefreshPresenceAsync();
        _presenceTimer.Start();
        _signalRService.MessageReceived += OnMessageReceived;
        _signalRService.TypingReceived += OnTypingReceived;
        _signalRService.OnlineStatusChanged += OnOnlineStatusChanged;
        _signalRService.FriendRequestReceived += OnFriendRequestReceived;
        _signalRService.FriendRequestResponded += OnFriendRequestResponded;
        _signalRService.GroupUpdated += OnGroupUpdated;
    }

    private async Task LoadChatListAsync()
    {
        try
        {
            var chats = await _chatApiService.GetChatsAsync();
            var previews = new List<SecureChat.Shared.DTOs.Chat.ChatSummaryDto>();
            foreach (var chat in chats)
            {
                previews.Add(await ResolvePlaintextPreviewAsync(chat));
            }

            if (InvokeRequired)
            {
                BeginInvoke(() => _leftSidebarPanel.SetChats(previews));
                return;
            }

            _leftSidebarPanel.SetChats(previews);
        }
        catch
        {
            NotificationHelper.ShowToast("SecureChat", "Unable to load chats.");
        }
    }

    private async Task LoadFriendListAsync()
    {
        List<FriendDto> friends;
        List<FriendRequestDto> requests = new();

        try
        {
            friends = await _friendApiService.GetFriendsAsync();
        }
        catch (Exception ex)
        {
            _ = ex;
            if (InvokeRequired)
            {
                BeginInvoke(() =>
                {
                    _leftSidebarPanel.SetFriends(Array.Empty<FriendDto>());
                    _leftSidebarPanel.SetFriendRequestCount(0);
                });
                return;
            }

            _leftSidebarPanel.SetFriends(Array.Empty<FriendDto>());
            _leftSidebarPanel.SetFriendRequestCount(0);
            return;
        }

        try
        {
            requests = await _friendApiService.GetFriendRequestsAsync();
        }
        catch
        {
            requests = new List<FriendRequestDto>();
        }

        if (InvokeRequired)
        {
            BeginInvoke(() =>
            {
                _leftSidebarPanel.SetFriends(friends);
                _leftSidebarPanel.SetFriendRequestCount(requests.Count);
            });
            return;
        }

        _leftSidebarPanel.SetFriends(friends);
        _leftSidebarPanel.SetFriendRequestCount(requests.Count);
    }

    private async Task RefreshPresenceAsync()
    {
        try
        {
            await _userApiService.PingPresenceAsync();
        }
        catch
        {
            // Friend list refresh still gives the user the latest known status if ping fails.
        }

        await LoadFriendListAsync();
    }

    private async void LeftSidebarChatSelected(object? sender, ChatSelectedEventArgs e)
    {
        _activeChat = e.Chat;
        _activeChatId = e.Chat.Id;
        _activeChatDetails = null;
        _leftSidebarPanel.MarkChatRead(e.Chat.Id);
        _rightInfoPanel.ShowUser(e.Chat);
        if (e.Chat.IsGroup)
        {
            try
            {
                var details = await _chatApiService.GetChatAsync(e.Chat.Id);
                _activeChatDetails = details;
                _rightInfoPanel.ShowGroup(e.Chat, details.Members.Count, details.Members.Select(member => member.DisplayName));
            }
            catch
            {
                _rightInfoPanel.ShowGroup(e.Chat);
            }
        }

        await _chatAreaPanel.LoadChatAsync(e.Chat);
        _leftSidebarPanel.MarkChatRead(e.Chat.Id);
    }

    private async void LeftSidebarFriendSelected(object? sender, FriendSelectedEventArgs e)
    {
        try
        {
            var chat = await _chatApiService.GetOrCreatePersonalChatAsync(e.Friend.UserId);
            var summary = new SecureChat.Shared.DTOs.Chat.ChatSummaryDto
            {
                Id = chat.Id,
                GroupId = chat.GroupId,
                Name = e.Friend.DisplayName,
                AvatarUrl = e.Friend.AvatarUrl,
                LastMessagePreview = chat.LastMessage?.EncryptedContent ?? string.Empty,
                LastMessageAt = chat.LastMessage?.SentAt,
                IsGroup = false
            };

            _activeChat = summary;
            _activeChatId = summary.Id;
            _activeChatDetails = chat;
            _rightInfoPanel.ShowUser(summary);
            await _chatAreaPanel.LoadChatAsync(summary);
            MarkActiveChatRead(summary.Id);
            await LoadChatListAsync();
        }
        catch (Exception ex)
        {
            NotificationHelper.ShowToast("SecureChat", ex.Message);
        }
    }

    private async void LeftSidebarChatDeleteRequested(object? sender, ChatSelectedEventArgs e)
    {
        var result = MessageBox.Show(
            this,
            $"Delete chat '{e.Chat.Name}' from your chat list?",
            "Delete chat",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);
        if (result != DialogResult.Yes)
        {
            return;
        }

        try
        {
            await _chatApiService.DeleteChatAsync(e.Chat.Id);
            _sessionCache.RemoveSessionKey(e.Chat.Id.ToString());
            if (_activeChatId == e.Chat.Id)
            {
                ClearActiveChat();
            }

            await LoadChatListAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Delete chat failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private async void FriendSearchRequested()
    {
        using var form = new FriendManagerForm(_friendApiService, FriendManagerMode.Search);
        form.ShowDialog(this);
        await LoadFriendListAsync();
    }

    private async void FriendRequestsRequested()
    {
        using var form = new FriendManagerForm(_friendApiService, FriendManagerMode.Requests);
        form.ShowDialog(this);
        await LoadFriendListAsync();
    }

    private async void BlockUserRequested()
    {
        using var form = new FriendManagerForm(_friendApiService, FriendManagerMode.Block);
        form.ShowDialog(this);
        await LoadFriendListAsync();
    }

    private async void GroupManagementRequested()
    {
        using var form = new GroupManagerForm(
            _serviceProvider.GetRequiredService<GroupApiService>(),
            _friendApiService);
        form.ShowDialog(this);
        await LoadChatListAsync();
    }

    private void SettingsRequested()
    {
        using var form = new SettingsForm(_serviceProvider);
        form.ShowDialog(this);
    }

    private async void RightInfoPrimaryActionRequested()
    {
        if (_activeChat is null)
        {
            return;
        }

        if (_activeChat.IsGroup)
        {
            await AddMemberToActiveGroupAsync();
            return;
        }

        await BlockActiveContactAsync();
    }

    private async void RightInfoSecondaryActionRequested()
    {
        if (_activeChat is null)
        {
            return;
        }

        if (_activeChat.IsGroup)
        {
            await LeaveActiveGroupAsync();
            return;
        }

        await _chatAreaPanel.ShowSharedFilesAsync();
    }

    private async void OnMessageReceived(object? sender, MessageDto message)
    {
        if (InvokeRequired)
        {
            BeginInvoke(() => OnMessageReceived(sender, message));
            return;
        }

        var preview = await _chatAreaPanel.GetPlaintextPreviewAsync(message);
        await _chatAreaPanel.AddReceivedMessageAsync(message);
        var isActive = _activeChatId == message.ChatId;
        _leftSidebarPanel.UpdateChatPreview(message.ChatId, preview, message.SentAt, !isActive);
        if (isActive)
        {
            MarkActiveChatRead(message.ChatId);
        }
        else
        {
            _ = LoadChatListAsync();
        }
        NotificationHelper.ShowToast("New message", string.IsNullOrWhiteSpace(message.SenderName) ? "Encrypted message received." : $"From {message.SenderName}");
        NotificationHelper.PlayMessageSound();
    }

    private async void ChatAreaPanelMessageSent(object? sender, MessageDto message)
    {
        if (InvokeRequired)
        {
            BeginInvoke(() => ChatAreaPanelMessageSent(sender, message));
            return;
        }

        var preview = await _chatAreaPanel.GetPlaintextPreviewAsync(message);
        _leftSidebarPanel.UpdateChatPreview(message.ChatId, preview, message.SentAt, incrementUnread: false);
        MarkActiveChatRead(message.ChatId);
    }

    private async Task<SecureChat.Shared.DTOs.Chat.ChatSummaryDto> ResolvePlaintextPreviewAsync(SecureChat.Shared.DTOs.Chat.ChatSummaryDto chat)
    {
        if (chat.LastMessageAt is null)
        {
            return chat;
        }

        try
        {
            var newest = (await _chatApiService.GetMessagesAsync(chat.Id, 1))
                .OrderByDescending(message => message.SentAt)
                .FirstOrDefault();
            if (newest is null)
            {
                return chat;
            }

            var preview = await _chatAreaPanel.GetPlaintextPreviewAsync(newest);
            return chat with { LastMessagePreview = preview, UnreadCount = _activeChatId == chat.Id ? 0 : chat.UnreadCount };
        }
        catch
        {
            return chat with { LastMessagePreview = "[Encrypted message]", UnreadCount = _activeChatId == chat.Id ? 0 : chat.UnreadCount };
        }
    }

    private void MarkActiveChatRead(Guid chatId)
    {
        _leftSidebarPanel.MarkChatRead(chatId);
    }

    private async Task BlockActiveContactAsync()
    {
        if (_activeChatDetails is null && _activeChat is not null)
        {
            _activeChatDetails = await _chatApiService.GetChatAsync(_activeChat.Id);
        }

        var other = _activeChatDetails?.Members.FirstOrDefault(member => member.Id != _currentUserId);
        if (other is null)
        {
            NotificationHelper.ShowToast("SecureChat", "Unable to find the contact to block.");
            return;
        }

        var result = MessageBox.Show(this, $"Block {other.DisplayName}?", "Block user", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
        if (result != DialogResult.Yes)
        {
            return;
        }

        await _friendApiService.BlockUserAsync(other.Id);
        _sessionCache.ClearAll();
        NotificationHelper.ShowToast("Blocked", $"{other.DisplayName} is blocked.");
        await LoadFriendListAsync();
        await LoadChatListAsync();
    }

    private async Task AddMemberToActiveGroupAsync()
    {
        if (_activeChat is null)
        {
            return;
        }

        var groupId = await ResolveActiveGroupIdAsync();
        if (groupId is null)
        {
            NotificationHelper.ShowToast("Group", "Unable to resolve group id for this chat.");
            return;
        }

        var groupApi = _serviceProvider.GetRequiredService<GroupApiService>();
        var details = _activeChatDetails ?? await _chatApiService.GetChatAsync(_activeChat.Id);
        _activeChatDetails = details;
        var existingIds = details.Members.Select(member => member.Id).ToHashSet();
        var candidates = (await _friendApiService.GetFriendsAsync())
            .Where(friend => !existingIds.Contains(friend.UserId))
            .ToList();
        if (candidates.Count == 0)
        {
            MessageBox.Show(this, "No friend can be added to this group.", "Add member", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var form = new Form
        {
            Text = "Add member",
            StartPosition = FormStartPosition.CenterParent,
            Size = new Size(420, 420),
            BackColor = ThemeHelper.BgPrimary,
            ForeColor = ThemeHelper.TextPrimary
        };
        var list = new CheckedListBox
        {
            Dock = DockStyle.Fill,
            BackColor = ThemeHelper.BgCard,
            ForeColor = ThemeHelper.TextPrimary,
            BorderStyle = BorderStyle.FixedSingle
        };
        foreach (var candidate in candidates)
        {
            list.Items.Add($"{candidate.DisplayName} ({(candidate.IsOnline ? "online" : "offline")})");
        }

        var addButton = new Button
        {
            Text = "Add selected",
            Dock = DockStyle.Bottom,
            Height = 38,
            BackColor = ThemeHelper.BgCard,
            ForeColor = ThemeHelper.TextPrimary,
            FlatStyle = FlatStyle.Flat
        };
        addButton.Click += (_, _) => form.DialogResult = DialogResult.OK;
        form.Controls.Add(list);
        form.Controls.Add(addButton);
        if (form.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        var selectedIndexes = list.CheckedIndices.Cast<int>().ToList();
        if (selectedIndexes.Count == 0)
        {
            return;
        }

        try
        {
            foreach (var index in selectedIndexes)
            {
                await groupApi.AddMemberAsync(groupId.Value, candidates[index].UserId);
            }

            _sessionCache.ClearAll();
            NotificationHelper.ShowToast("Group updated", "Members added. A new session key will be exchanged.");
            await LoadChatListAsync();
            await RefreshActiveGroupDetailsAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Add member failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private async Task LeaveActiveGroupAsync()
    {
        if (_activeChat is null)
        {
            return;
        }

        var result = MessageBox.Show(this, $"Leave {_activeChat.Name}?", "Leave group", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
        if (result != DialogResult.Yes)
        {
            return;
        }

        var groupId = await ResolveActiveGroupIdAsync();
        if (groupId is null)
        {
            NotificationHelper.ShowToast("Group", "Unable to resolve group id for this chat.");
            return;
        }

        try
        {
            await _serviceProvider.GetRequiredService<GroupApiService>().LeaveGroupAsync(groupId.Value);
            _sessionCache.ClearAll();
            _activeChat = null;
            _activeChatId = null;
            _activeChatDetails = null;
            _rightInfoPanel.ShowEmpty();
            NotificationHelper.ShowToast("Group", "You left the group.");
            await LoadChatListAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Leave group failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private async Task<Guid?> ResolveActiveGroupIdAsync()
    {
        if (_activeChat?.GroupId is Guid activeGroupId)
        {
            return activeGroupId;
        }

        if (_activeChatDetails?.GroupId is Guid detailGroupId)
        {
            return detailGroupId;
        }

        if (_activeChat is null || !_activeChat.IsGroup)
        {
            return null;
        }

        try
        {
            _activeChatDetails = await _chatApiService.GetChatAsync(_activeChat.Id);
            return _activeChatDetails.GroupId;
        }
        catch
        {
            return null;
        }
    }

    private async Task RefreshActiveGroupDetailsAsync()
    {
        if (_activeChat is null || !_activeChat.IsGroup)
        {
            return;
        }

        try
        {
            var details = await _chatApiService.GetChatAsync(_activeChat.Id);
            _activeChatDetails = details;
            _activeChat = _activeChat with
            {
                GroupId = details.GroupId,
                Name = string.IsNullOrWhiteSpace(details.Name) ? _activeChat.Name : details.Name,
                AvatarUrl = string.IsNullOrWhiteSpace(details.AvatarUrl) ? _activeChat.AvatarUrl : details.AvatarUrl
            };
            _rightInfoPanel.ShowGroup(_activeChat, details.Members.Count, details.Members.Select(member => member.DisplayName));
            await _chatAreaPanel.RefreshActiveChatStatusAsync();
        }
        catch
        {
            ClearActiveChat();
        }
    }

    private void ClearActiveChat()
    {
        _activeChat = null;
        _activeChatId = null;
        _activeChatDetails = null;
        _rightInfoPanel.ShowEmpty();
        _chatAreaPanel.ClearChat();
    }

    private void OnTypingReceived(object? sender, TypingEventArgs e)
    {
        if (InvokeRequired)
        {
            BeginInvoke(() => OnTypingReceived(sender, e));
            return;
        }

        _chatAreaPanel.ShowTyping(e.DisplayName, e.IsTyping);
    }

    private void OnOnlineStatusChanged(object? sender, OnlineStatusEventArgs e)
    {
        if (InvokeRequired)
        {
            BeginInvoke(() => OnOnlineStatusChanged(sender, e));
            return;
        }

        if (!e.IsOnline)
        {
            _sessionCache.ClearAll();
            _chatAreaPanel.ResetActiveSessionKey();
        }

        _ = LoadFriendListAsync();
        _ = _chatAreaPanel.RefreshActiveChatStatusAsync();
    }

    private void OnFriendRequestReceived(object? sender, FriendRequestDto e)
    {
        if (InvokeRequired)
        {
            BeginInvoke(() => OnFriendRequestReceived(sender, e));
            return;
        }

        NotificationHelper.ShowToast("Friend request", e.Message);
        _ = LoadFriendListAsync();
    }

    private void OnFriendRequestResponded(object? sender, string e)
    {
        if (InvokeRequired)
        {
            BeginInvoke(() => OnFriendRequestResponded(sender, e));
            return;
        }

        NotificationHelper.ShowToast("Friend request", "Friend request response received.");
        _ = LoadFriendListAsync();
        _ = LoadChatListAsync();
    }

    private void OnGroupUpdated(object? sender, GroupUpdateEventArgs e)
    {
        if (InvokeRequired)
        {
            BeginInvoke(() => OnGroupUpdated(sender, e));
            return;
        }

        _sessionCache.ClearAll();
        _ = RefreshAfterGroupUpdateAsync(e);
    }

    private async Task RefreshAfterGroupUpdateAsync(GroupUpdateEventArgs e)
    {
        lock (_pendingGroupRefreshes)
        {
            if (!_pendingGroupRefreshes.Add(e.GroupId))
            {
                return;
            }
        }

        try
        {
            await Task.Delay(150);
            await LoadChatListAsync();
            await LoadFriendListAsync();
            if (_activeChat?.IsGroup == true && (_activeChat.GroupId == e.GroupId || _activeChatDetails?.GroupId == e.GroupId))
            {
                var dissolved = string.Equals(e.EventType, "dissolved", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(e.EventType, "ownerLeft", StringComparison.OrdinalIgnoreCase);
                if (dissolved)
                {
                    ClearActiveChat();
                    return;
                }

                await RefreshActiveGroupDetailsAsync();
            }
        }
        finally
        {
            lock (_pendingGroupRefreshes)
            {
                _pendingGroupRefreshes.Remove(e.GroupId);
            }
        }
    }

    private async void LogoutRequested()
    {
        try
        {
            await _authController.LogoutAsync();
        }
        catch
        {
            await _signalRService.DisconnectAsync();
            var account = _authSession.CurrentAccount;
            _authSession.Clear();
            await _keyStorageService.ClearTokensAsync(account);
        }

        Hide();
        var loginForm = new LoginForm(_serviceProvider);
        loginForm.FormClosed += (_, _) => Close();
        loginForm.Show();
    }

    private static Guid? ExtractGuidClaim(string? accessToken, string claimName)
    {
        var value = ExtractStringClaim(accessToken, claimName);
        return Guid.TryParse(value, out var id) ? id : null;
    }

    private static string? ExtractStringClaim(string? accessToken, string claimName)
    {
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return null;
        }

        var parts = accessToken.Split('.');
        if (parts.Length < 2)
        {
            return null;
        }

        try
        {
            var payload = parts[1].Replace('-', '+').Replace('_', '/');
            payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');
            var json = Encoding.UTF8.GetString(Convert.FromBase64String(payload));
            using var document = JsonDocument.Parse(json);
            return document.RootElement.TryGetProperty(claimName, out var value) ? value.GetString() : null;
        }
        catch (Exception ex) when (ex is FormatException or JsonException)
        {
            return null;
        }
    }
}
