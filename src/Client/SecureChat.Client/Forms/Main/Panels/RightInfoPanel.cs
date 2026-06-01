#nullable enable

using SecureChat.Client.Forms.Helpers;
using SecureChat.Shared.DTOs.Chat;

namespace SecureChat.Client.Forms.Main.Panels;

/// <summary>
/// Displays chat participant or group details.
/// </summary>
public class RightInfoPanel : UserControl
{
    private readonly Label _titleLabel;
    private readonly Label _subtitleLabel;
    private readonly Label _badgeLabel;
    private readonly TextBox _fingerprintBox;
    private readonly FlowLayoutPanel _detailsPanel;
    private readonly Button _primaryButton;
    private readonly Button _secondaryButton;

    /// <summary>
    /// Initializes a new instance of the <see cref="RightInfoPanel"/> class.
    /// </summary>
    public RightInfoPanel()
    {
        DoubleBuffered = true;
        Dock = DockStyle.Fill;
        BackColor = ThemeHelper.BgSecondary;

        _titleLabel = new Label();
        _subtitleLabel = new Label();
        _badgeLabel = new Label();
        _fingerprintBox = new TextBox();
        _detailsPanel = new FlowLayoutPanel();
        _primaryButton = new Button();
        _secondaryButton = new Button();
        BuildControls();
    }

    /// <summary>Occurs when the main action button is clicked.</summary>
    public event Action? PrimaryActionRequested;

    /// <summary>Occurs when the secondary action button is clicked.</summary>
    public event Action? SecondaryActionRequested;

    /// <summary>Shows the neutral empty state.</summary>
    public void ShowEmpty()
    {
        _titleLabel.Text = "Select a chat";
        _subtitleLabel.Text = string.Empty;
        _badgeLabel.Text = "SecureChat";
        _fingerprintBox.Text = string.Empty;
        _primaryButton.Text = "Action";
        _secondaryButton.Text = "Action";
        _detailsPanel.Controls.Clear();
        Invalidate();
    }

    /// <summary>Shows user mode details.</summary>
    public void ShowUser(ChatSummaryDto chat, string email = "")
    {
        _titleLabel.Text = chat.Name;
        _subtitleLabel.Text = string.IsNullOrWhiteSpace(email) ? "SecureChat contact" : email;
        _badgeLabel.Text = "E2EE Active";
        _fingerprintBox.Text = "Open Security Center to view your device fingerprint.";
        _primaryButton.Text = "Block User";
        _secondaryButton.Text = "Shared Files";
        _detailsPanel.Controls.Clear();
        _detailsPanel.Controls.Add(CreateMutedLabel("Shared Files"));
        _detailsPanel.Controls.Add(CreateMutedLabel("No shared files yet"));
        Invalidate();
    }

    /// <summary>Shows group mode details.</summary>
    public void ShowGroup(ChatSummaryDto chat, int memberCount = 0, IEnumerable<string>? memberNames = null)
    {
        _titleLabel.Text = chat.Name;
        _subtitleLabel.Text = $"{memberCount} members";
        _badgeLabel.Text = "Group E2EE";
        _fingerprintBox.Text = "Rotating group key";
        _primaryButton.Text = "Add Member";
        _secondaryButton.Text = "Leave Group";
        _detailsPanel.Controls.Clear();
        _detailsPanel.Controls.Add(CreateMutedLabel("Members"));
        if (memberNames is not null)
        {
            foreach (var member in memberNames)
            {
                _detailsPanel.Controls.Add(CreateMutedLabel(member));
            }
        }

        Invalidate();
    }

    /// <inheritdoc />
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        using var avatar = new SolidBrush(ThemeHelper.AccentBlue);
        e.Graphics.FillEllipse(avatar, (Width - 80) / 2, 28, 80, 80);
        var initial = string.IsNullOrWhiteSpace(_titleLabel.Text) ? "S" : _titleLabel.Text[0].ToString().ToUpperInvariant();
        TextRenderer.DrawText(e.Graphics, initial, new Font("Segoe UI", 24F, FontStyle.Bold), new Rectangle((Width - 80) / 2, 28, 80, 80), Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
    }

    private void BuildControls()
    {
        _titleLabel.SetBounds(16, 120, Width - 32, 28);
        _titleLabel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        _titleLabel.Font = new Font("Segoe UI", 13F, FontStyle.Bold);
        _titleLabel.ForeColor = ThemeHelper.TextPrimary;
        _titleLabel.TextAlign = ContentAlignment.MiddleCenter;
        _titleLabel.Text = "Select a chat";

        _subtitleLabel.SetBounds(16, 150, Width - 32, 22);
        _subtitleLabel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        _subtitleLabel.ForeColor = ThemeHelper.TextMuted;
        _subtitleLabel.TextAlign = ContentAlignment.MiddleCenter;

        _badgeLabel.SetBounds(70, 188, Width - 140, 26);
        _badgeLabel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        _badgeLabel.BackColor = Color.FromArgb(29, 82, 45);
        _badgeLabel.ForeColor = ThemeHelper.AccentGreen;
        _badgeLabel.TextAlign = ContentAlignment.MiddleCenter;
        _badgeLabel.Text = "E2EE Active";

        _fingerprintBox.SetBounds(18, 238, Width - 36, 56);
        _fingerprintBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        _fingerprintBox.Multiline = true;
        _fingerprintBox.ReadOnly = true;
        _fingerprintBox.BorderStyle = BorderStyle.None;
        _fingerprintBox.BackColor = ThemeHelper.BgCard;
        _fingerprintBox.ForeColor = ThemeHelper.TextSecondary;
        _fingerprintBox.Font = new Font("Consolas", 10F);

        _detailsPanel.SetBounds(18, 320, Width - 36, Height - 430);
        _detailsPanel.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        _detailsPanel.AutoScroll = true;
        _detailsPanel.FlowDirection = FlowDirection.TopDown;
        _detailsPanel.WrapContents = false;
        _detailsPanel.BackColor = ThemeHelper.BgSecondary;

        _primaryButton.SetBounds(18, Height - 92, Width - 36, 34);
        _primaryButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        ConfigureButton(_primaryButton);
        _primaryButton.Text = "Block User";
        _primaryButton.Click += (_, _) => PrimaryActionRequested?.Invoke();

        _secondaryButton.SetBounds(18, Height - 50, Width - 36, 34);
        _secondaryButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        ConfigureButton(_secondaryButton);
        _secondaryButton.Text = "Settings";
        _secondaryButton.Click += (_, _) => SecondaryActionRequested?.Invoke();

        Controls.Add(_secondaryButton);
        Controls.Add(_primaryButton);
        Controls.Add(_detailsPanel);
        Controls.Add(_fingerprintBox);
        Controls.Add(_badgeLabel);
        Controls.Add(_subtitleLabel);
        Controls.Add(_titleLabel);
    }

    private static Label CreateMutedLabel(string text)
    {
        return new Label
        {
            Text = text,
            ForeColor = ThemeHelper.TextMuted,
            AutoSize = false,
            Width = 220,
            Height = 26,
            Margin = new Padding(0, 0, 0, 6)
        };
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
