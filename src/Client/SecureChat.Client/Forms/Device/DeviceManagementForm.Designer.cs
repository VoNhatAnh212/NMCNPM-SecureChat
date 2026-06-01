#nullable enable

using SecureChat.Client.Forms.Helpers;

namespace SecureChat.Client.Forms.Device;

/// <summary>
/// Designer portion of <see cref="DeviceManagementForm"/>.
/// </summary>
public partial class DeviceManagementForm
{
    private Label _titleLabel = null!;
    private Label _statusLabel = null!;
    private Button _refreshButton = null!;
    private ListView _deviceList = null!;

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        SuspendLayout();

        _titleLabel = new Label
        {
            Text = "Trusted Devices",
            Dock = DockStyle.Top,
            Height = 44,
            Font = new Font("Segoe UI", 18F, FontStyle.Bold),
            ForeColor = ThemeHelper.TextPrimary
        };

        _refreshButton = new Button
        {
            Text = "Refresh",
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Location = new Point(436, 18),
            Size = new Size(88, 30),
            FlatStyle = FlatStyle.Flat,
            BackColor = ThemeHelper.BgCard,
            ForeColor = ThemeHelper.TextPrimary
        };
        _refreshButton.FlatAppearance.BorderColor = ThemeHelper.Border;
        _refreshButton.Click += RefreshButtonClick;

        _statusLabel = new Label
        {
            Text = "Loading devices...",
            Dock = DockStyle.Bottom,
            Height = 26,
            ForeColor = ThemeHelper.TextSecondary
        };

        _deviceList = new ListView
        {
            Dock = DockStyle.Fill,
            View = View.Details,
            OwnerDraw = true,
            HeaderStyle = ColumnHeaderStyle.None,
            FullRowSelect = true,
            BorderStyle = BorderStyle.None,
            BackColor = ThemeHelper.BgPrimary,
            ForeColor = ThemeHelper.TextPrimary,
            SmallImageList = new ImageList { ImageSize = new Size(1, 96) }
        };
        _deviceList.Columns.Add(string.Empty, 500);
        _deviceList.DrawItem += DeviceListDrawItem;
        _deviceList.MouseClick += DeviceListMouseClick;
        _deviceList.Resize += (_, _) => _deviceList.Columns[0].Width = Math.Max(100, _deviceList.ClientSize.Width - 4);

        Controls.Add(_deviceList);
        Controls.Add(_statusLabel);
        Controls.Add(_refreshButton);
        Controls.Add(_titleLabel);

        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        BackColor = ThemeHelper.BgPrimary;
        ClientSize = new Size(560, 500);
        ForeColor = ThemeHelper.TextPrimary;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Name = "DeviceManagementForm";
        Padding = new Padding(18);
        StartPosition = FormStartPosition.CenterParent;
        Text = "Device Management";
        Load += DeviceManagementFormLoad;

        ResumeLayout(false);
    }
}
