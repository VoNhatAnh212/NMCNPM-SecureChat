#nullable enable

using SecureChat.Client.Forms.Helpers;
using SecureChat.Client.Forms.Main.Panels;
using Microsoft.Extensions.DependencyInjection;
using SecureChat.Client.Services.Api;
using SecureChat.Client.Services.Crypto;
using SecureChat.Client.Services.SignalR;
using SecureChat.Client.Services.Storage;

namespace SecureChat.Client.Forms.Main;

/// <summary>
/// Designer portion of <see cref="MainForm"/>.
/// </summary>
public partial class MainForm
{
    private TableLayoutPanel _rootLayout = null!;
    private LeftSidebarPanel _leftSidebarPanel = null!;
    private ChatAreaPanel _chatAreaPanel = null!;
    private RightInfoPanel _rightInfoPanel = null!;

    /// <summary>
    /// Releases resources used by the form.
    /// </summary>
    /// <param name="disposing">A value indicating whether managed resources should be disposed.</param>
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        SuspendLayout();

        _rootLayout = new TableLayoutPanel
        {
            ColumnCount = 3,
            Dock = DockStyle.Fill,
            BackColor = ThemeHelper.BgPrimary,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        _rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
        _rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55F));
        _rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
        _rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        _leftSidebarPanel = new LeftSidebarPanel();
        _leftSidebarPanel.ChatSelected += LeftSidebarChatSelected;
        _leftSidebarPanel.FriendSelected += LeftSidebarFriendSelected;
        _leftSidebarPanel.LogoutRequested += LogoutRequested;
        _leftSidebarPanel.FriendSearchRequested += FriendSearchRequested;
        _leftSidebarPanel.FriendRequestsRequested += FriendRequestsRequested;
        _leftSidebarPanel.BlockUserRequested += BlockUserRequested;
        _leftSidebarPanel.GroupManagementRequested += GroupManagementRequested;

        _chatAreaPanel = new ChatAreaPanel(
            _serviceProvider.GetRequiredService<ChatApiService>(),
            _serviceProvider.GetRequiredService<UserApiService>(),
            _serviceProvider.GetRequiredService<ISignalRService>(),
            _serviceProvider.GetRequiredService<AesGcmService>(),
            _serviceProvider.GetRequiredService<FileApiService>(),
            _serviceProvider.GetRequiredService<RsaService>(),
            _serviceProvider.GetRequiredService<KeyStorageService>(),
            _serviceProvider.GetRequiredService<SessionCache>(),
            _serviceProvider.GetRequiredService<LocalMessageStore>());

        _rightInfoPanel = new RightInfoPanel();
        _rightInfoPanel.PrimaryActionRequested += RightInfoPrimaryActionRequested;
        _rightInfoPanel.SecondaryActionRequested += RightInfoSecondaryActionRequested;

        _rootLayout.Controls.Add(_leftSidebarPanel, 0, 0);
        _rootLayout.Controls.Add(_chatAreaPanel, 1, 0);
        _rootLayout.Controls.Add(_rightInfoPanel, 2, 0);

        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        BackColor = ThemeHelper.BgPrimary;
        ClientSize = new Size(1400, 850);
        Controls.Add(_rootLayout);
        MinimumSize = new Size(1100, 650);
        Name = "MainForm";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "SecureChat";

        ResumeLayout(false);
    }
}
