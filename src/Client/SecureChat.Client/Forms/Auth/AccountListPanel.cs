#nullable enable

using System.Drawing.Drawing2D;
using SecureChat.Client.Forms.Helpers;
using SecureChat.Client.Models;

namespace SecureChat.Client.Forms.Auth;

/// <summary>
/// Displays saved local accounts using owner-drawn rounded rows.
/// </summary>
public class AccountListPanel : Panel
{
    private const int ItemHeight = 74;
    private const int ItemMargin = 8;
    private readonly List<LocalUser> _accounts = new();
    private int _hoverIndex = -1;
    private int _selectedIndex = -1;

    /// <summary>
    /// Initializes a new instance of the <see cref="AccountListPanel"/> class.
    /// </summary>
    public AccountListPanel()
    {
        DoubleBuffered = true;
        AutoScroll = true;
        BackColor = ThemeHelper.BgSecondary;
        TabStop = true;
    }

    /// <summary>Occurs when the selected account changes.</summary>
    public event EventHandler<LocalUser?>? SelectedAccountChanged;

    /// <summary>Occurs when an account is activated.</summary>
    public event EventHandler<LocalUser>? AccountActivated;

    /// <summary>Gets the selected account.</summary>
    public LocalUser? SelectedAccount => _selectedIndex >= 0 && _selectedIndex < _accounts.Count ? _accounts[_selectedIndex] : null;

    /// <summary>
    /// Loads accounts into the panel.
    /// </summary>
    /// <param name="accounts">The accounts to display.</param>
    public void SetAccounts(IEnumerable<LocalUser> accounts)
    {
        _accounts.Clear();
        _accounts.AddRange(accounts);
        _selectedIndex = _accounts.Count > 0 ? 0 : -1;
        AutoScrollMinSize = new Size(0, Math.Max(0, _accounts.Count * (ItemHeight + ItemMargin) + ItemMargin));
        SelectedAccountChanged?.Invoke(this, SelectedAccount);
        Invalidate();
    }

    /// <inheritdoc />
    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        var index = HitTest(e.Location);
        if (_hoverIndex != index)
        {
            _hoverIndex = index;
            Invalidate();
        }
    }

    /// <inheritdoc />
    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        _hoverIndex = -1;
        Invalidate();
    }

    /// <inheritdoc />
    protected override void OnMouseClick(MouseEventArgs e)
    {
        base.OnMouseClick(e);
        var index = HitTest(e.Location);
        if (index < 0)
        {
            return;
        }

        _selectedIndex = index;
        SelectedAccountChanged?.Invoke(this, SelectedAccount);
        Focus();
        Invalidate();
    }

    /// <inheritdoc />
    protected override void OnMouseDoubleClick(MouseEventArgs e)
    {
        base.OnMouseDoubleClick(e);
        var index = HitTest(e.Location);
        if (index >= 0)
        {
            AccountActivated?.Invoke(this, _accounts[index]);
        }
    }

    /// <inheritdoc />
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.KeyCode == Keys.Enter && SelectedAccount is not null)
        {
            AccountActivated?.Invoke(this, SelectedAccount);
            e.Handled = true;
            return;
        }

        if (e.KeyCode == Keys.Down && _accounts.Count > 0)
        {
            _selectedIndex = Math.Min(_accounts.Count - 1, _selectedIndex + 1);
            SelectedAccountChanged?.Invoke(this, SelectedAccount);
            Invalidate();
            e.Handled = true;
        }

        if (e.KeyCode == Keys.Up && _accounts.Count > 0)
        {
            _selectedIndex = Math.Max(0, _selectedIndex - 1);
            SelectedAccountChanged?.Invoke(this, SelectedAccount);
            Invalidate();
            e.Handled = true;
        }
    }

    /// <inheritdoc />
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.TranslateTransform(AutoScrollPosition.X, AutoScrollPosition.Y);

        for (var i = 0; i < _accounts.Count; i++)
        {
            DrawAccount(e.Graphics, _accounts[i], i, GetItemRect(i));
        }
    }

    private void DrawAccount(Graphics graphics, LocalUser account, int index, Rectangle rect)
    {
        var fill = index == _selectedIndex ? Color.FromArgb(38, 57, 78) : index == _hoverIndex ? ThemeHelper.BgHover : ThemeHelper.BgCard;
        ThemeHelper.DrawRoundedRect(graphics, rect, 10, fill);

        using var borderPath = ThemeHelper.CreateRoundedRectanglePath(rect, 10);
        using var border = new Pen(index == _selectedIndex ? ThemeHelper.AccentBlue : ThemeHelper.Border);
        graphics.DrawPath(border, borderPath);

        var avatarRect = new Rectangle(rect.Left + 14, rect.Top + 17, 40, 40);
        DrawAvatar(graphics, avatarRect, account.DisplayName);

        var nameRect = new Rectangle(rect.Left + 68, rect.Top + 12, rect.Width - 84, 22);
        var deviceRect = new Rectangle(rect.Left + 68, rect.Top + 34, rect.Width - 84, 18);
        var lastLoginRect = new Rectangle(rect.Left + 68, rect.Top + 52, rect.Width - 84, 16);

        TextRenderer.DrawText(graphics, account.DisplayName, new Font("Segoe UI", 10F, FontStyle.Bold), nameRect, ThemeHelper.TextPrimary, TextFormatFlags.EndEllipsis);
        TextRenderer.DrawText(graphics, account.DeviceName, new Font("Segoe UI", 8.5F), deviceRect, ThemeHelper.TextSecondary, TextFormatFlags.EndEllipsis);
        TextRenderer.DrawText(graphics, $"Last login: {account.LastLoginAt:g}", new Font("Segoe UI", 8F), lastLoginRect, ThemeHelper.TextMuted, TextFormatFlags.EndEllipsis);
    }

    private static void DrawAvatar(Graphics graphics, Rectangle rect, string displayName)
    {
        using var brush = new SolidBrush(ThemeHelper.AccentBlue);
        graphics.FillEllipse(brush, rect);
        var initial = string.IsNullOrWhiteSpace(displayName) ? "?" : displayName.Trim()[0].ToString().ToUpperInvariant();
        TextRenderer.DrawText(graphics, initial, new Font("Segoe UI", 14F, FontStyle.Bold), rect, Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
    }

    private int HitTest(Point point)
    {
        var translatedY = point.Y - AutoScrollPosition.Y;
        for (var i = 0; i < _accounts.Count; i++)
        {
            if (GetItemRect(i).Contains(point.X - AutoScrollPosition.X, translatedY))
            {
                return i;
            }
        }

        return -1;
    }

    private Rectangle GetItemRect(int index)
    {
        return new Rectangle(ItemMargin, ItemMargin + index * (ItemHeight + ItemMargin), Math.Max(10, ClientSize.Width - ItemMargin * 2 - SystemInformation.VerticalScrollBarWidth), ItemHeight);
    }
}
