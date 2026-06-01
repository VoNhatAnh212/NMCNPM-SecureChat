#nullable enable

using SecureChat.Client.Forms.Helpers;
using SecureChat.Client.Services.Api;
using SecureChat.Shared.DTOs.Friend;
using SecureChat.Shared.DTOs.Group;

namespace SecureChat.Client.Forms.Groups;

/// <summary>
/// Provides basic group creation and member management.
/// </summary>
public class GroupManagerForm : Form
{
    private readonly GroupApiService _groupApiService;
    private readonly FriendApiService _friendApiService;
    private readonly TextBox _nameTextBox = new();
    private readonly CheckedListBox _friendsListBox = new();
    private readonly ListBox _groupsListBox = new();
    private readonly ListBox _membersListBox = new();
    private readonly Button _createButton = new();
    private readonly Button _addButton = new();
    private readonly Button _removeButton = new();
    private readonly Label _statusLabel = new();
    private readonly List<FriendDto> _friends = new();
    private readonly List<GroupDto> _groups = new();
    private GroupDto? _selectedGroup;

    /// <summary>Initializes a new instance of the <see cref="GroupManagerForm"/> class.</summary>
    public GroupManagerForm(GroupApiService groupApiService, FriendApiService friendApiService)
    {
        _groupApiService = groupApiService;
        _friendApiService = friendApiService;
        BuildControls();
        ThemeHelper.ApplyTheme(this);
    }

    /// <inheritdoc />
    protected override async void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        await ReloadAsync();
    }

    private void BuildControls()
    {
        Text = "Groups";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(700, 520);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;

        Controls.Add(new Label { Text = "Group name", Location = new Point(18, 16), Size = new Size(180, 22) });
        _nameTextBox.SetBounds(18, 42, 300, 28);
        _createButton.Text = "Create";
        _createButton.SetBounds(330, 41, 90, 30);
        _createButton.Click += async (_, _) => await CreateGroupAsync();

        Controls.Add(new Label { Text = "Friends", Location = new Point(18, 86), Size = new Size(180, 22) });
        _friendsListBox.SetBounds(18, 112, 300, 320);

        Controls.Add(new Label { Text = "Groups", Location = new Point(340, 86), Size = new Size(180, 22) });
        _groupsListBox.SetBounds(340, 112, 330, 130);
        _groupsListBox.SelectedIndexChanged += async (_, _) => await LoadSelectedGroupAsync();

        Controls.Add(new Label { Text = "Members", Location = new Point(340, 256), Size = new Size(180, 22) });
        _membersListBox.SetBounds(340, 282, 330, 150);

        _addButton.Text = "Add checked friends";
        _addButton.SetBounds(18, 448, 150, 32);
        _addButton.Click += async (_, _) => await AddCheckedFriendsAsync();
        _removeButton.Text = "Remove selected";
        _removeButton.SetBounds(340, 448, 140, 32);
        _removeButton.Click += async (_, _) => await RemoveSelectedMemberAsync();
        _statusLabel.SetBounds(18, 488, 652, 24);
        _statusLabel.ForeColor = ThemeHelper.TextMuted;
        _nameTextBox.BackColor = ThemeHelper.BgCard;
        _nameTextBox.ForeColor = ThemeHelper.TextPrimary;
        foreach (var list in new ListBox[] { _friendsListBox, _groupsListBox, _membersListBox })
        {
            list.BackColor = ThemeHelper.BgCard;
            list.ForeColor = ThemeHelper.TextPrimary;
            list.BorderStyle = BorderStyle.FixedSingle;
        }

        foreach (var button in new[] { _createButton, _addButton, _removeButton })
        {
            ConfigureButton(button);
        }

        Controls.Add(_nameTextBox);
        Controls.Add(_createButton);
        Controls.Add(_friendsListBox);
        Controls.Add(_groupsListBox);
        Controls.Add(_membersListBox);
        Controls.Add(_addButton);
        Controls.Add(_removeButton);
        Controls.Add(_statusLabel);
    }

    private async Task ReloadAsync()
    {
        _friends.Clear();
        _groups.Clear();
        _friendsListBox.Items.Clear();
        _groupsListBox.Items.Clear();
        _membersListBox.Items.Clear();

        _friends.AddRange(await _friendApiService.GetFriendsAsync());
        foreach (var friend in _friends)
        {
            _friendsListBox.Items.Add($"{friend.DisplayName} ({(friend.IsOnline ? "online" : "offline")})");
        }

        _groups.AddRange(await _groupApiService.GetGroupsAsync());
        foreach (var group in _groups)
        {
            _groupsListBox.Items.Add($"{group.Name} ({group.MemberCount})");
        }
    }

    private async Task CreateGroupAsync()
    {
        try
        {
            var memberIds = _friendsListBox.CheckedIndices.Cast<int>().Select(index => _friends[index].UserId).ToList();
            await _groupApiService.CreateGroupAsync(new CreateGroupDto { Name = _nameTextBox.Text.Trim(), MemberIds = memberIds });
            _statusLabel.Text = "Group created. A group chat will appear in Chats.";
            await ReloadAsync();
        }
        catch (Exception ex)
        {
            _statusLabel.Text = ex.Message;
        }
    }

    private async Task LoadSelectedGroupAsync()
    {
        if (_groupsListBox.SelectedIndex < 0)
        {
            return;
        }

        _selectedGroup = await _groupApiService.GetGroupDetailsAsync(_groups[_groupsListBox.SelectedIndex].Id);
        _membersListBox.Items.Clear();
        foreach (var member in _selectedGroup.Members)
        {
            _membersListBox.Items.Add($"{member.DisplayName} - {member.Role}");
        }
    }

    private async Task AddCheckedFriendsAsync()
    {
        if (_selectedGroup is null)
        {
            _statusLabel.Text = "Select a group first.";
            return;
        }

        try
        {
            foreach (var index in _friendsListBox.CheckedIndices.Cast<int>())
            {
                await _groupApiService.AddMemberAsync(_selectedGroup.Id, _friends[index].UserId);
            }

            _statusLabel.Text = "Members added. Group key rotation required.";
            await LoadSelectedGroupAsync();
        }
        catch (Exception ex)
        {
            _statusLabel.Text = ex.Message;
        }
    }

    private async Task RemoveSelectedMemberAsync()
    {
        if (_selectedGroup is null || _membersListBox.SelectedIndex < 0)
        {
            _statusLabel.Text = "Select a member first.";
            return;
        }

        try
        {
            var member = _selectedGroup.Members.ElementAt(_membersListBox.SelectedIndex);
            await _groupApiService.RemoveMemberAsync(_selectedGroup.Id, member.UserId);
            _statusLabel.Text = "Member removed. Group key rotation required.";
            await LoadSelectedGroupAsync();
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
