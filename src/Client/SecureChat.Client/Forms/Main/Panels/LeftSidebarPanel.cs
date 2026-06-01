#nullable enable

using System.ComponentModel;
using System.Drawing.Drawing2D;
using SecureChat.Client.Forms.Controls;
using SecureChat.Client.Forms.Helpers;
using SecureChat.Shared.DTOs.Friend;
using SecureChat.Shared.DTOs.Chat;

namespace SecureChat.Client.Forms.Main.Panels;

/// <summary>
/// Displays user status, search, tabs, and chat summaries.
/// </summary>
public class LeftSidebarPanel : UserControl
{
    private const int UserBarHeight = 64;
    private const int SearchHeight = 64;
    private const int TabBarHeight = 42;
    private const int BottomBarHeight = 112;
    private const int ItemHeight = 74;
    private readonly RoundedTextBox _searchTextBox;
    private readonly Button _clearButton;
    private readonly ChatListSurface _chatListSurface;
    private readonly Label _displayNameLabel;
    private readonly Label _statusLabel;
    private readonly Button _settingsButton;
    private readonly Button _logoutButton;
    private readonly Button _chatsTabButton;
    private readonly Button _friendsTabButton;
    private readonly Button _groupsTabButton;
    private readonly Button _blockUserButton;
    private readonly Button _findFriendButton;
    private readonly Button _friendRequestsButton;
    private readonly Button _createGroupButton;
    private readonly List<ChatSummaryDto> _chatSummaries = new();
    private readonly List<FriendDto> _friends = new();
    private readonly Dictionary<Guid, int> _localUnreadCounts = new();
    private Guid? _activeReadChatId;
    private Guid _currentUserId;
    private string _activeTab = "Chats";

    /// <summary>
    /// Initializes a new instance of the <see cref="LeftSidebarPanel"/> class.
    /// </summary>
    public LeftSidebarPanel()
    {
        DoubleBuffered = true;
        BackColor = ThemeHelper.BgSecondary;
        Dock = DockStyle.Fill;

        _displayNameLabel = new Label();
        _statusLabel = new Label();
        _settingsButton = new Button();
        _logoutButton = new Button();
        _chatsTabButton = new Button();
        _friendsTabButton = new Button();
        _groupsTabButton = new Button();
        _searchTextBox = new RoundedTextBox();
        _clearButton = new Button();
        _chatListSurface = new ChatListSurface();
        _blockUserButton = new Button();
        _findFriendButton = new Button();
        _friendRequestsButton = new Button();
        _createGroupButton = new Button();

        BuildControls();
    }

    /// <summary>Occurs when a chat is selected.</summary>
    public event EventHandler<ChatSelectedEventArgs>? ChatSelected;

    /// <summary>Occurs when a friend is selected.</summary>
    public event EventHandler<FriendSelectedEventArgs>? FriendSelected;

    /// <summary>Occurs when a chat delete action is requested.</summary>
    public event EventHandler<ChatSelectedEventArgs>? ChatDeleteRequested;

    /// <summary>Occurs when settings are requested.</summary>
    public event Action? SettingsRequested;

    /// <summary>Occurs when logout is requested.</summary>
    public event Action? LogoutRequested;

    /// <summary>Occurs when friend search is requested.</summary>
    public event Action? FriendSearchRequested;

    /// <summary>Occurs when friend requests are requested.</summary>
    public event Action? FriendRequestsRequested;

    /// <summary>Occurs when block user management is requested.</summary>
    public event Action? BlockUserRequested;

    /// <summary>Occurs when group management is requested.</summary>
    public event Action? GroupManagementRequested;

    /// <summary>Updates the visible incoming request count.</summary>
    public void SetFriendRequestCount(int count)
    {
        _friendRequestsButton.Text = count > 0 ? $"Requests ({Math.Min(99, count)})" : "Requests";
        _friendRequestsButton.BackColor = count > 0 ? Color.FromArgb(52, ThemeHelper.AccentGreen) : Color.FromArgb(34, ThemeHelper.AccentGreen);
    }

    /// <summary>Sets the current user display name.</summary>
    public void SetCurrentUser(string displayName)
    {
        _displayNameLabel.Text = string.IsNullOrWhiteSpace(displayName) ? "SecureChat User" : displayName;
    }

    /// <summary>Sets the current user identity shown in the sidebar.</summary>
    public void SetCurrentUser(Guid userId, string displayName)
    {
        _currentUserId = userId;
        SetCurrentUser(displayName);
    }

    /// <summary>Loads chat summaries into the sidebar.</summary>
    public void SetChats(IEnumerable<ChatSummaryDto> chats)
    {
        _chatSummaries.Clear();
        _chatSummaries.AddRange(chats.Select(chat => chat with
        {
            UnreadCount = _localUnreadCounts.GetValueOrDefault(chat.Id)
        }));
        RenderActiveList();
    }

    /// <summary>Loads friends into the sidebar.</summary>
    public void SetFriends(IEnumerable<FriendDto> friends)
    {
        _friends.Clear();
        _friends.AddRange(friends
            .Where(friend => friend.UserId != Guid.Empty && friend.UserId != _currentUserId)
            .GroupBy(friend => friend.UserId)
            .Select(group => group.First())
            .OrderBy(friend => friend.DisplayName));
        RenderActiveList();
    }

    /// <summary>Updates a friend's online status.</summary>
    public void UpdateFriendOnlineStatus(Guid userId, bool isOnline)
    {
        var index = _friends.FindIndex(friend => friend.UserId == userId);
        if (index < 0)
        {
            return;
        }

        _friends[index] = _friends[index] with { IsOnline = isOnline };
        if (_activeTab == "Friends")
        {
            RenderActiveList();
        }
    }

    /// <summary>Updates a chat item after a received message.</summary>
    public void UpdateChatPreview(Guid chatId, string preview, DateTimeOffset sentAt, bool incrementUnread = true)
    {
        if (incrementUnread && _activeReadChatId != chatId)
        {
            _localUnreadCounts[chatId] = _localUnreadCounts.GetValueOrDefault(chatId) + 1;
        }
        else
        {
            _localUnreadCounts[chatId] = 0;
        }

        _chatListSurface.UpdateChatPreview(chatId, preview, sentAt, _localUnreadCounts.GetValueOrDefault(chatId));
    }

    /// <summary>Clears the unread badge for a chat after the user opens it.</summary>
    public void MarkChatRead(Guid chatId)
    {
        _activeReadChatId = chatId;
        _localUnreadCounts[chatId] = 0;
        _chatListSurface.MarkChatRead(chatId);
    }

    /// <summary>Updates online status text.</summary>
    public void SetOnline(bool online)
    {
        _statusLabel.Text = online ? "● Online" : "● Offline";
        _statusLabel.ForeColor = online ? ThemeHelper.AccentGreen : ThemeHelper.TextMuted;
    }

    /// <inheritdoc />
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        DrawUserInfo(e.Graphics);
    }

    private void BuildControls()
    {
        _displayNameLabel.SetBounds(64, 11, 112, 22);
        _displayNameLabel.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        _displayNameLabel.ForeColor = ThemeHelper.TextPrimary;
        _displayNameLabel.Text = "SecureChat User";

        _statusLabel.SetBounds(64, 35, 120, 18);
        _statusLabel.Font = new Font("Segoe UI", 8.5F);
        _statusLabel.Text = "● Online";
        _statusLabel.ForeColor = ThemeHelper.AccentGreen;
        _statusLabel.Cursor = Cursors.Hand;
        _statusLabel.Click += (_, _) => SetOnline(!_statusLabel.Text.Contains("Online", StringComparison.OrdinalIgnoreCase));

        _settingsButton.SetBounds(Width - 124, 17, 30, 30);
        _settingsButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        _settingsButton.Text = "⚙";
        ConfigureIconButton(_settingsButton);
        _settingsButton.Click += (_, _) => SettingsRequested?.Invoke();

        _logoutButton.SetBounds(Width - 86, 17, 72, 30);
        _logoutButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        _logoutButton.Text = "Logout";
        ConfigureIconButton(_logoutButton);
        _logoutButton.Click += (_, _) => LogoutRequested?.Invoke();

        _searchTextBox.SetBounds(14, UserBarHeight + 10, Width - 28, 36);
        _searchTextBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        _searchTextBox.PlaceholderText = "Search chats...";
        _searchTextBox.TextChanged += (_, _) =>
        {
            _clearButton.Visible = !string.IsNullOrWhiteSpace(_searchTextBox.Text);
            _chatListSurface.Filter(_searchTextBox.Text ?? string.Empty);
        };

        _clearButton.SetBounds(Width - 42, UserBarHeight + 15, 24, 24);
        _clearButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        _clearButton.Text = "X";
        _clearButton.Visible = false;
        ConfigureIconButton(_clearButton);
        _clearButton.Click += (_, _) => _searchTextBox.Text = string.Empty;

        var tabTop = UserBarHeight + SearchHeight;
        ConfigureTabButton(_chatsTabButton, "Chats");
        _chatsTabButton.SetBounds(0, tabTop, Width / 3, TabBarHeight);
        _chatsTabButton.Anchor = AnchorStyles.Top | AnchorStyles.Left;
        _chatsTabButton.Click += (_, _) => SetActiveTab("Chats");

        ConfigureTabButton(_friendsTabButton, "Friends");
        _friendsTabButton.SetBounds(Width / 3, tabTop, Width / 3, TabBarHeight);
        _friendsTabButton.Anchor = AnchorStyles.Top | AnchorStyles.Left;
        _friendsTabButton.Click += (_, _) => SetActiveTab("Friends");

        ConfigureTabButton(_groupsTabButton, "Groups");
        _groupsTabButton.SetBounds((Width / 3) * 2, tabTop, Width / 3, TabBarHeight);
        _groupsTabButton.Anchor = AnchorStyles.Top | AnchorStyles.Left;
        _groupsTabButton.Click += (_, _) => SetActiveTab("Groups");

        _chatListSurface.SetBounds(0, UserBarHeight + SearchHeight + TabBarHeight, Width, Height - UserBarHeight - SearchHeight - TabBarHeight - BottomBarHeight);
        _chatListSurface.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        _chatListSurface.ChatSelected += (_, chat) =>
        {
            if (_activeTab == "Friends")
            {
                var friend = _friends.FirstOrDefault(item => item.UserId == chat.Id);
                if (friend is not null)
                {
                    FriendSelected?.Invoke(this, new FriendSelectedEventArgs(friend));
                }

                return;
            }

            ChatSelected?.Invoke(this, new ChatSelectedEventArgs(chat));
        };
        _chatListSurface.ChatDeleteRequested += (_, chat) => ChatDeleteRequested?.Invoke(this, new ChatSelectedEventArgs(chat));

        _blockUserButton.Text = "Block";
        _blockUserButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        ConfigureActionButton(_blockUserButton, ThemeHelper.Danger);
        _blockUserButton.Click += (_, _) => BlockUserRequested?.Invoke();

        _findFriendButton.Text = "Find";
        _findFriendButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        ConfigureActionButton(_findFriendButton, ThemeHelper.AccentBlue);
        _findFriendButton.Click += (_, _) => FriendSearchRequested?.Invoke();

        _friendRequestsButton.Text = "Requests";
        _friendRequestsButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        ConfigureActionButton(_friendRequestsButton, ThemeHelper.AccentGreen);
        _friendRequestsButton.Click += (_, _) => FriendRequestsRequested?.Invoke();

        _createGroupButton.Text = "Group";
        _createGroupButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        ConfigureActionButton(_createGroupButton, Color.FromArgb(180, 132, 255));
        _createGroupButton.Click += (_, _) => GroupManagementRequested?.Invoke();
        LayoutActionButtons();
        LayoutTopSurfaces();

        Controls.Add(_blockUserButton);
        Controls.Add(_findFriendButton);
        Controls.Add(_friendRequestsButton);
        Controls.Add(_createGroupButton);
        Controls.Add(_chatListSurface);
        Controls.Add(_groupsTabButton);
        Controls.Add(_friendsTabButton);
        Controls.Add(_chatsTabButton);
        Controls.Add(_clearButton);
        Controls.Add(_searchTextBox);
        Controls.Add(_logoutButton);
        Controls.Add(_settingsButton);
        Controls.Add(_statusLabel);
        Controls.Add(_displayNameLabel);
        UpdateTabButtons();
    }

    /// <inheritdoc />
    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        var tabTop = UserBarHeight + SearchHeight;
        var tabWidth = Math.Max(1, Width / 3);
        _chatsTabButton.SetBounds(0, tabTop, tabWidth, TabBarHeight);
        _friendsTabButton.SetBounds(tabWidth, tabTop, tabWidth, TabBarHeight);
        _groupsTabButton.SetBounds(tabWidth * 2, tabTop, Width - tabWidth * 2, TabBarHeight);
        LayoutTopSurfaces();
        LayoutActionButtons();
    }

    private void DrawUserInfo(Graphics graphics)
    {
        using var avatarBrush = new SolidBrush(ThemeHelper.AccentBlue);
        graphics.FillEllipse(avatarBrush, new Rectangle(14, 12, 40, 40));
        TextRenderer.DrawText(graphics, "S", new Font("Segoe UI", 14F, FontStyle.Bold), new Rectangle(14, 12, 40, 40), Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        using var borderPen = new Pen(ThemeHelper.Border);
        graphics.DrawLine(borderPen, 0, UserBarHeight - 1, Width, UserBarHeight - 1);
    }

    private void SetActiveTab(string tab)
    {
        _activeTab = tab;
        _searchTextBox.PlaceholderText = tab == "Friends" ? "Search friends..." : "Search chats...";
        RenderActiveList();
        UpdateTabButtons();
        Invalidate();
    }

    private void RenderActiveList()
    {
        if (_activeTab == "Friends")
        {
            _chatListSurface.AllowItemMenu = false;
            _chatListSurface.EmptyMessage = "No friends to show";
            _chatListSurface.SetChats(_friends.Select(friend => new ChatSummaryDto
            {
                Id = friend.UserId,
                Name = friend.DisplayName,
                AvatarUrl = friend.AvatarUrl,
                LastMessagePreview = friend.IsOnline ? "Online" : "Offline",
                IsGroup = false
            }));
            return;
        }

        _chatListSurface.AllowItemMenu = _activeTab == "Chats";
        _chatListSurface.EmptyMessage = _activeTab == "Groups" ? "No groups yet" : "No chats yet";
        _chatListSurface.SetChats(_activeTab == "Groups"
            ? _chatSummaries.Where(chat => chat.IsGroup)
            : _chatSummaries);
    }

    private static void ConfigureIconButton(Button button)
    {
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 0;
        button.BackColor = ThemeHelper.BgSecondary;
        button.ForeColor = ThemeHelper.TextSecondary;
        button.Cursor = Cursors.Hand;
    }

    private static void ConfigureActionButton(Button button, Color accent)
    {
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderColor = Color.FromArgb(70, accent);
        button.FlatAppearance.BorderSize = 1;
        button.BackColor = Color.FromArgb(34, accent);
        button.ForeColor = ThemeHelper.TextPrimary;
        button.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
        button.Cursor = Cursors.Hand;
    }

    private void LayoutActionButtons()
    {
        var left = 14;
        var gap = 8;
        var buttonWidth = Math.Max(64, (Width - left * 2 - gap) / 2);
        var top = Height - 98;
        _blockUserButton.SetBounds(left, top, buttonWidth, 38);
        _findFriendButton.SetBounds(left + buttonWidth + gap, top, buttonWidth, 38);
        _friendRequestsButton.SetBounds(left, top + 46, buttonWidth, 38);
        _createGroupButton.SetBounds(left + buttonWidth + gap, top + 46, buttonWidth, 38);
    }

    private void LayoutTopSurfaces()
    {
        _settingsButton.SetBounds(Math.Max(1, Width - 124), 17, 30, 30);
        _logoutButton.SetBounds(Math.Max(1, Width - 86), 17, 72, 30);
        _searchTextBox.SetBounds(14, UserBarHeight + 10, Math.Max(1, Width - 28), 36);
        _clearButton.SetBounds(Math.Max(1, Width - 42), UserBarHeight + 15, 24, 24);
        _chatListSurface.SetBounds(0, UserBarHeight + SearchHeight + TabBarHeight, Width, Math.Max(1, Height - UserBarHeight - SearchHeight - TabBarHeight - BottomBarHeight));
    }

    private static void ConfigureTabButton(Button button, string text)
    {
        button.Text = text;
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 0;
        button.BackColor = ThemeHelper.BgSecondary;
        button.ForeColor = ThemeHelper.TextMuted;
        button.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
        button.Cursor = Cursors.Hand;
    }

    private void UpdateTabButtons()
    {
        UpdateTabButton(_chatsTabButton, _activeTab == "Chats");
        UpdateTabButton(_friendsTabButton, _activeTab == "Friends");
        UpdateTabButton(_groupsTabButton, _activeTab == "Groups");
    }

    private static void UpdateTabButton(Button button, bool active)
    {
        button.BackColor = active ? ThemeHelper.BgCard : ThemeHelper.BgSecondary;
        button.ForeColor = active ? ThemeHelper.TextPrimary : ThemeHelper.TextMuted;
        button.Font = new Font("Segoe UI", 9F, active ? FontStyle.Bold : FontStyle.Regular);
    }

    private sealed class ChatListSurface : Panel
    {
        private readonly List<ChatSummaryDto> _allChats = new();
        private readonly List<ChatSummaryDto> _visibleChats = new();
        private int _hoverIndex = -1;
        private int _selectedIndex = -1;

        public ChatListSurface()
        {
            DoubleBuffered = true;
            AutoScroll = true;
            BackColor = ThemeHelper.BgSecondary;
        }

        public event EventHandler<ChatSummaryDto>? ChatSelected;

        public event EventHandler<ChatSummaryDto>? ChatDeleteRequested;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string EmptyMessage { get; set; } = "No chats yet";

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool AllowItemMenu { get; set; }

        public void SetChats(IEnumerable<ChatSummaryDto> chats)
        {
            _allChats.Clear();
            _allChats.AddRange(chats
                .OrderByDescending(chat => chat.LastMessageAt.HasValue)
                .ThenByDescending(chat => chat.LastMessageAt)
                .ThenBy(chat => chat.Name));
            Filter(string.Empty);
        }

        public void Filter(string query)
        {
            _visibleChats.Clear();
            _visibleChats.AddRange(string.IsNullOrWhiteSpace(query)
                ? _allChats
                : _allChats.Where(chat => chat.Name.Contains(query, StringComparison.OrdinalIgnoreCase)));
            AutoScrollMinSize = new Size(0, _visibleChats.Count * ItemHeight);
            Invalidate();
        }

        public void UpdateChatPreview(Guid chatId, string preview, DateTimeOffset sentAt, int unreadCount)
        {
            var index = _allChats.FindIndex(chat => chat.Id == chatId);
            if (index < 0)
            {
                return;
            }

            _allChats[index] = _allChats[index] with
            {
                LastMessagePreview = preview,
                LastMessageAt = sentAt,
                UnreadCount = unreadCount
            };
            Filter(string.Empty);
        }

        public void MarkChatRead(Guid chatId)
        {
            var index = _allChats.FindIndex(chat => chat.Id == chatId);
            if (index < 0)
            {
                return;
            }

            _allChats[index] = _allChats[index] with { UnreadCount = 0 };
            Filter(string.Empty);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            var index = HitTest(e.Y - AutoScrollPosition.Y);
            if (_hoverIndex != index)
            {
                _hoverIndex = index;
                Invalidate();
            }
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);
            var index = HitTest(e.Y - AutoScrollPosition.Y);
            if (index < 0)
            {
                return;
            }

            if (AllowItemMenu && GetOptionsRectangle(index).Contains(e.Location.X, e.Location.Y - AutoScrollPosition.Y))
            {
                ShowItemMenu(index, e.Location);
                return;
            }

            _selectedIndex = index;
            ChatSelected?.Invoke(this, _visibleChats[index]);
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            if (_visibleChats.Count == 0)
            {
                TextRenderer.DrawText(
                    e.Graphics,
                    EmptyMessage,
                    new Font("Segoe UI", 10F, FontStyle.Regular),
                    ClientRectangle,
                    ThemeHelper.TextMuted,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
                return;
            }

            e.Graphics.TranslateTransform(0, AutoScrollPosition.Y);
            var first = Math.Max(0, (-AutoScrollPosition.Y) / ItemHeight);
            var last = Math.Min(_visibleChats.Count - 1, first + Height / ItemHeight + 2);
            for (var i = first; i <= last; i++)
            {
                DrawItem(e.Graphics, _visibleChats[i], i);
            }
        }

        private void DrawItem(Graphics graphics, ChatSummaryDto chat, int index)
        {
            var rect = new Rectangle(8, index * ItemHeight + 6, Width - 24, ItemHeight - 10);
            var fill = index == _selectedIndex ? ThemeHelper.BgCard : index == _hoverIndex ? ThemeHelper.BgHover : ThemeHelper.BgSecondary;
            ThemeHelper.DrawRoundedRect(graphics, rect, 8, fill);
            if (index == _selectedIndex)
            {
                using var pen = new Pen(ThemeHelper.AccentBlue, 4F);
                graphics.DrawLine(pen, rect.Left, rect.Top + 8, rect.Left, rect.Bottom - 8);
            }

            using var avatarBrush = new SolidBrush(chat.IsGroup ? ThemeHelper.AccentGreen : ThemeHelper.AccentBlue);
            graphics.FillEllipse(avatarBrush, rect.Left + 12, rect.Top + 10, 46, 46);
            TextRenderer.DrawText(graphics, chat.Name[..Math.Min(1, chat.Name.Length)].ToUpperInvariant(), new Font("Segoe UI", 13F, FontStyle.Bold), new Rectangle(rect.Left + 12, rect.Top + 10, 46, 46), Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            TextRenderer.DrawText(graphics, chat.Name, new Font("Segoe UI", 10F, FontStyle.Bold), new Rectangle(rect.Left + 70, rect.Top + 10, rect.Width - 136, 22), ThemeHelper.TextPrimary, TextFormatFlags.EndEllipsis);
            TextRenderer.DrawText(graphics, chat.LastMessagePreview, new Font("Segoe UI", 8.5F), new Rectangle(rect.Left + 70, rect.Top + 34, rect.Width - 100, 20), ThemeHelper.TextMuted, TextFormatFlags.EndEllipsis);
            var time = chat.LastMessageAt?.LocalDateTime.ToShortTimeString() ?? string.Empty;
            TextRenderer.DrawText(graphics, time, new Font("Segoe UI", 8F), new Rectangle(rect.Right - 64, rect.Top + 12, 56, 18), ThemeHelper.TextMuted, TextFormatFlags.Right);
            if (AllowItemMenu && (index == _hoverIndex || index == _selectedIndex))
            {
                var optionsRect = GetOptionsRectangle(index);
                TextRenderer.DrawText(graphics, "⋮", new Font("Segoe UI", 15F, FontStyle.Bold), optionsRect, ThemeHelper.TextSecondary, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            }

            if (chat.UnreadCount > 0)
            {
                using var badge = new SolidBrush(ThemeHelper.Danger);
                graphics.FillEllipse(badge, rect.Right - 32, rect.Top + 36, 22, 22);
                TextRenderer.DrawText(graphics, Math.Min(99, chat.UnreadCount).ToString(), new Font("Segoe UI", 7.5F, FontStyle.Bold), new Rectangle(rect.Right - 32, rect.Top + 36, 22, 22), Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            }
        }

        private int HitTest(int y)
        {
            var index = y / ItemHeight;
            return index >= 0 && index < _visibleChats.Count ? index : -1;
        }

        private Rectangle GetOptionsRectangle(int index)
        {
            var rect = new Rectangle(8, index * ItemHeight + 6, Width - 24, ItemHeight - 10);
            return new Rectangle(rect.Right - 36, rect.Top + 8, 28, 28);
        }

        private void ShowItemMenu(int index, Point location)
        {
            var menu = new ContextMenuStrip
            {
                BackColor = ThemeHelper.BgCard,
                ForeColor = ThemeHelper.TextPrimary
            };
            var deleteItem = new ToolStripMenuItem("Delete chat");
            deleteItem.Click += (_, _) => ChatDeleteRequested?.Invoke(this, _visibleChats[index]);
            menu.Items.Add(deleteItem);
            menu.Show(this, location);
        }
    }
}

/// <summary>
/// Provides selected chat event data.
/// </summary>
public sealed class ChatSelectedEventArgs : EventArgs
{
    /// <summary>Initializes a new instance of the <see cref="ChatSelectedEventArgs"/> class.</summary>
    public ChatSelectedEventArgs(ChatSummaryDto chat)
    {
        Chat = chat;
    }

    /// <summary>Gets the selected chat summary.</summary>
    public ChatSummaryDto Chat { get; }
}

/// <summary>
/// Provides selected friend event data.
/// </summary>
public sealed class FriendSelectedEventArgs : EventArgs
{
    /// <summary>Initializes a new instance of the <see cref="FriendSelectedEventArgs"/> class.</summary>
    public FriendSelectedEventArgs(FriendDto friend)
    {
        Friend = friend;
    }

    /// <summary>Gets the selected friend.</summary>
    public FriendDto Friend { get; }
}
