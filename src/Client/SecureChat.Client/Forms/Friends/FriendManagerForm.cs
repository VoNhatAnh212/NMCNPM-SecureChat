#nullable enable

using SecureChat.Client.Forms.Helpers;
using SecureChat.Client.Services.Api;
using SecureChat.Shared.DTOs.Friend;
using SecureChat.Shared.DTOs.User;

namespace SecureChat.Client.Forms.Friends;

/// <summary>
/// Lets users search people, send friend requests, and accept pending requests.
/// </summary>
public class FriendManagerForm : Form
{
    private readonly FriendManagerMode _mode;
    private readonly FriendApiService _friendApiService;
    private readonly Label _searchLabel = new();
    private readonly Label _requestsLabel = new();
    private readonly Label _sentRequestsLabel = new();
    private readonly Label _blockedLabel = new();
    private readonly TextBox _searchTextBox = new();
    private readonly Button _searchButton = new();
    private readonly ListBox _resultsListBox = new();
    private readonly Button _sendRequestButton = new();
    private readonly Button _blockButton = new();
    private readonly ListBox _requestsListBox = new();
    private readonly ListBox _sentRequestsListBox = new();
    private readonly Button _acceptButton = new();
    private readonly Button _rejectButton = new();
    private readonly ListBox _blockedListBox = new();
    private readonly Button _unblockButton = new();
    private readonly Label _statusLabel = new();
    private readonly List<UserDto> _results = new();
    private readonly List<FriendRequestDto> _requests = new();
    private readonly List<FriendRequestDto> _sentRequests = new();
    private readonly List<BlockedUserDto> _blockedUsers = new();

    /// <summary>Initializes a new instance of the <see cref="FriendManagerForm"/> class.</summary>
    public FriendManagerForm(FriendApiService friendApiService, FriendManagerMode mode = FriendManagerMode.All)
    {
        _friendApiService = friendApiService;
        _mode = mode;
        BuildControls();
        ThemeHelper.ApplyTheme(this);
    }

    /// <inheritdoc />
    protected override async void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        try
        {
            await LoadRequestsAsync();
            if (_mode == FriendManagerMode.Block)
            {
                await LoadBlockCandidatesAsync();
            }
        }
        catch (Exception ex)
        {
            _statusLabel.Text = ex.Message;
        }
    }

    private void BuildControls()
    {
        Text = "Friends";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(560, 610);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;

        _searchLabel.Text = "Find user";
        _searchLabel.SetBounds(18, 16, 220, 22);
        _searchTextBox.SetBounds(18, 42, 390, 28);
        _searchButton.Text = "Search";
        _searchButton.SetBounds(420, 41, 112, 30);
        _searchButton.Click += async (_, _) => await SearchAsync();

        _resultsListBox.SetBounds(18, 82, 514, 116);
        _sendRequestButton.Text = "Send friend request";
        _sendRequestButton.SetBounds(18, 206, 180, 32);
        _sendRequestButton.Click += async (_, _) => await SendRequestAsync();
        _blockButton.Text = "Block user";
        _blockButton.SetBounds(210, 206, 120, 32);
        _blockButton.Click += async (_, _) => await BlockSelectedUserAsync();

        _requestsLabel.Text = "Incoming requests";
        _requestsLabel.SetBounds(18, 252, 220, 22);
        _requestsListBox.SetBounds(18, 278, 514, 92);
        _acceptButton.Text = "Accept";
        _acceptButton.SetBounds(18, 382, 90, 32);
        _acceptButton.Click += async (_, _) => await RespondAsync(true);
        _rejectButton.Text = "Reject";
        _rejectButton.SetBounds(116, 382, 90, 32);
        _rejectButton.Click += async (_, _) => await RespondAsync(false);

        _sentRequestsLabel.Text = "Sent requests";
        _sentRequestsLabel.SetBounds(18, 426, 220, 22);
        _sentRequestsListBox.SetBounds(18, 452, 514, 42);
        _blockedLabel.Text = "Blocked users";
        _blockedLabel.SetBounds(18, 506, 220, 22);
        _blockedListBox.SetBounds(18, 532, 390, 42);
        _unblockButton.Text = "Unblock";
        _unblockButton.SetBounds(420, 532, 112, 32);
        _unblockButton.Click += async (_, _) => await UnblockSelectedUserAsync();
        _statusLabel.SetBounds(18, 582, 514, 24);
        _statusLabel.ForeColor = ThemeHelper.TextMuted;
        foreach (var button in new[] { _searchButton, _sendRequestButton, _blockButton, _acceptButton, _rejectButton, _unblockButton })
        {
            ConfigureButton(button);
        }

        foreach (var list in new[] { _resultsListBox, _requestsListBox, _sentRequestsListBox, _blockedListBox })
        {
            list.BackColor = ThemeHelper.BgCard;
            list.ForeColor = ThemeHelper.TextPrimary;
            list.BorderStyle = BorderStyle.FixedSingle;
        }

        _searchTextBox.BackColor = ThemeHelper.BgCard;
        _searchTextBox.ForeColor = ThemeHelper.TextPrimary;

        Controls.Add(_searchLabel);
        Controls.Add(_searchTextBox);
        Controls.Add(_searchButton);
        Controls.Add(_resultsListBox);
        Controls.Add(_sendRequestButton);
        Controls.Add(_blockButton);
        Controls.Add(_requestsLabel);
        Controls.Add(_requestsListBox);
        Controls.Add(_acceptButton);
        Controls.Add(_rejectButton);
        Controls.Add(_sentRequestsLabel);
        Controls.Add(_sentRequestsListBox);
        Controls.Add(_blockedLabel);
        Controls.Add(_blockedListBox);
        Controls.Add(_unblockButton);
        Controls.Add(_statusLabel);
        ApplyMode();
    }

    private void ApplyMode()
    {
        if (_mode == FriendManagerMode.All)
        {
            return;
        }

        Text = _mode switch
        {
            FriendManagerMode.Search => "Find friends",
            FriendManagerMode.Requests => "Friend requests",
            FriendManagerMode.Block => "Block users",
            _ => Text
        };

        var searchControls = new Control[] { _searchLabel, _searchTextBox, _searchButton, _resultsListBox, _sendRequestButton };
        var requestControls = new Control[] { _requestsLabel, _requestsListBox, _acceptButton, _rejectButton, _sentRequestsLabel, _sentRequestsListBox };
        var blockControls = new Control[] { _blockButton, _blockedLabel, _blockedListBox, _unblockButton };

        foreach (var control in searchControls.Concat(requestControls).Concat(blockControls))
        {
            control.Visible = false;
        }

        if (_mode == FriendManagerMode.Search)
        {
            ClientSize = new Size(560, 300);
            _searchLabel.SetBounds(18, 16, 220, 22);
            _searchTextBox.SetBounds(18, 42, 390, 30);
            _searchButton.SetBounds(420, 41, 112, 32);
            _resultsListBox.SetBounds(18, 86, 514, 126);
            _sendRequestButton.SetBounds(18, 224, 180, 34);
            _statusLabel.SetBounds(18, 266, 514, 24);
            foreach (var control in searchControls)
            {
                control.Visible = true;
            }
        }
        else if (_mode == FriendManagerMode.Requests)
        {
            ClientSize = new Size(560, 430);
            _requestsLabel.SetBounds(18, 16, 220, 22);
            _requestsListBox.SetBounds(18, 42, 514, 150);
            _acceptButton.SetBounds(18, 206, 100, 34);
            _rejectButton.SetBounds(128, 206, 100, 34);
            _sentRequestsLabel.SetBounds(18, 258, 220, 22);
            _sentRequestsListBox.SetBounds(18, 284, 514, 88);
            _statusLabel.SetBounds(18, 392, 514, 24);
            foreach (var control in requestControls)
            {
                control.Visible = true;
            }
        }
        else if (_mode == FriendManagerMode.Block)
        {
            ClientSize = new Size(560, 470);
            _searchLabel.Text = "Find user to block";
            _searchLabel.SetBounds(18, 16, 220, 22);
            _searchTextBox.SetBounds(18, 42, 390, 30);
            _searchButton.SetBounds(420, 41, 112, 32);
            _resultsListBox.SetBounds(18, 86, 514, 130);
            _blockButton.SetBounds(18, 228, 120, 34);
            _blockedLabel.SetBounds(18, 278, 220, 22);
            _blockedListBox.SetBounds(18, 304, 390, 88);
            _unblockButton.SetBounds(420, 304, 112, 34);
            _statusLabel.SetBounds(18, 430, 514, 24);
            _searchLabel.Visible = true;
            _searchTextBox.Visible = true;
            _searchButton.Visible = true;
            _resultsListBox.Visible = true;
            _blockButton.Visible = true;
            foreach (var control in blockControls)
            {
                control.Visible = true;
            }
        }
    }

    private async Task SearchAsync()
    {
        try
        {
            _results.Clear();
            _resultsListBox.Items.Clear();
            var users = await _friendApiService.SearchUsersAsync(_searchTextBox.Text.Trim());
            _results.AddRange(users);
            foreach (var user in _results)
            {
                _resultsListBox.Items.Add($"{user.DisplayName} <{user.Email}>");
            }

            _statusLabel.Text = $"{_results.Count} result(s)";
        }
        catch (Exception ex)
        {
            _statusLabel.Text = ex.Message;
        }
    }

    private async Task LoadBlockCandidatesAsync()
    {
        _results.Clear();
        _resultsListBox.Items.Clear();
        var friends = await _friendApiService.GetFriendsAsync();
        _results.AddRange(friends.Select(friend => new UserDto
        {
            Id = friend.UserId,
            DisplayName = friend.DisplayName,
            AvatarUrl = friend.AvatarUrl,
            IsOnline = friend.IsOnline
        }));

        foreach (var user in _results)
        {
            _resultsListBox.Items.Add($"{user.DisplayName} {(user.IsOnline ? "(online)" : "(offline)")}");
        }
    }

    private async Task SendRequestAsync()
    {
        if (_resultsListBox.SelectedIndex < 0)
        {
            _statusLabel.Text = "Select a user first.";
            return;
        }

        var user = _results[_resultsListBox.SelectedIndex];
        try
        {
            await _friendApiService.SendFriendRequestAsync(user.Id, "Let's connect on SecureChat.");
            _statusLabel.Text = "Friend request sent.";
            await LoadRequestsAsync();
        }
        catch (Exception ex)
        {
            _statusLabel.Text = ex.Message;
        }
    }

    private async Task BlockSelectedUserAsync()
    {
        if (_resultsListBox.SelectedIndex < 0)
        {
            _statusLabel.Text = "Select a user first.";
            return;
        }

        var user = _results[_resultsListBox.SelectedIndex];
        var result = MessageBox.Show(this, $"Block {user.DisplayName}?", "Block user", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
        if (result != DialogResult.Yes)
        {
            return;
        }

        try
        {
            await _friendApiService.BlockUserAsync(user.Id);
            _statusLabel.Text = "User blocked.";
            await LoadRequestsAsync();
            if (string.IsNullOrWhiteSpace(_searchTextBox.Text))
            {
                await LoadBlockCandidatesAsync();
            }
            else
            {
                await SearchAsync();
            }
        }
        catch (Exception ex)
        {
            _statusLabel.Text = ex.Message;
        }
    }

    private async Task LoadRequestsAsync()
    {
        _requests.Clear();
        _sentRequests.Clear();
        _blockedUsers.Clear();
        _requestsListBox.Items.Clear();
        _sentRequestsListBox.Items.Clear();
        _blockedListBox.Items.Clear();
        var requests = await _friendApiService.GetFriendRequestsAsync();
        var sentRequests = await _friendApiService.GetSentFriendRequestsAsync();
        var blockedUsers = await _friendApiService.GetBlockedUsersAsync();
        _requests.AddRange(requests);
        _sentRequests.AddRange(sentRequests);
        _blockedUsers.AddRange(blockedUsers);
        foreach (var request in _requests)
        {
            _requestsListBox.Items.Add($"{request.FromUser.DisplayName}: {request.Message}");
        }

        foreach (var request in _sentRequests)
        {
            _sentRequestsListBox.Items.Add($"To {request.ToUser.DisplayName} <{request.ToUser.Email}>");
        }

        foreach (var blocked in _blockedUsers)
        {
            _blockedListBox.Items.Add(blocked.DisplayName);
        }
    }

    private async Task UnblockSelectedUserAsync()
    {
        if (_blockedListBox.SelectedIndex < 0)
        {
            _statusLabel.Text = "Select a blocked user first.";
            return;
        }

        try
        {
            await _friendApiService.UnblockUserAsync(_blockedUsers[_blockedListBox.SelectedIndex].UserId);
            _statusLabel.Text = "User unblocked.";
            await LoadRequestsAsync();
            if (_mode == FriendManagerMode.Block && string.IsNullOrWhiteSpace(_searchTextBox.Text))
            {
                await LoadBlockCandidatesAsync();
            }
        }
        catch (Exception ex)
        {
            _statusLabel.Text = ex.Message;
        }
    }

    private async Task RespondAsync(bool accept)
    {
        if (_requestsListBox.SelectedIndex < 0)
        {
            _statusLabel.Text = "Select a request first.";
            return;
        }

        var request = _requests[_requestsListBox.SelectedIndex];
        try
        {
            if (accept)
            {
                await _friendApiService.AcceptRequestAsync(request.Id);
                _statusLabel.Text = "Friend request accepted.";
            }
            else
            {
                await _friendApiService.RejectRequestAsync(request.Id);
                _statusLabel.Text = "Friend request rejected.";
            }

            await LoadRequestsAsync();
        }
        catch (Exception ex)
        {
            _statusLabel.Text = ex.Message;
        }
    }

    private static void ConfigureButton(Button button)
    {
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderColor = ThemeHelper.Border;
        button.BackColor = ThemeHelper.BgCard;
        button.ForeColor = ThemeHelper.TextPrimary;
        button.Cursor = Cursors.Hand;
    }
}

/// <summary>
/// Defines which friend management workflow should be shown.
/// </summary>
public enum FriendManagerMode
{
    /// <summary>Shows all workflows.</summary>
    All,
    /// <summary>Shows friend search and outgoing requests.</summary>
    Search,
    /// <summary>Shows incoming request accept/reject workflow.</summary>
    Requests,
    /// <summary>Shows block/unblock workflow.</summary>
    Block
}
