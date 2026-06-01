#nullable enable

using System.ComponentModel;
using System.Drawing.Drawing2D;
using SecureChat.Client.Forms.Helpers;
using SecureChat.Shared.DTOs.File;
using SecureChat.Shared.DTOs.Message;
using SecureChat.Shared.Enums;

namespace SecureChat.Client.Forms.Main.Controls;

/// <summary>
/// Renders a single chat message bubble.
/// </summary>
public class MessageBubbleControl : UserControl
{
    private const int HorizontalPadding = 14;
    private const int VerticalPadding = 10;
    private const int BubbleRadius = 18;
    private const int MaxBubbleWidth = 420;
    private MessageDto? _message;
    private string _plainText = string.Empty;

    /// <summary>
    /// Initializes a new instance of the <see cref="MessageBubbleControl"/> class.
    /// </summary>
    public MessageBubbleControl()
    {
        DoubleBuffered = true;
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        BackColor = Color.Transparent;
        ContextMenuStrip = BuildContextMenu();
        Width = 680;
        Height = 72;
    }

    /// <summary>Occurs when an attachment is clicked.</summary>
    public event EventHandler<AttachmentDto>? AttachmentClicked;

    /// <summary>Gets or sets the source message.</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public MessageDto? Message
    {
        get => _message;
        set
        {
            _message = value;
            RecalculateHeight();
            Invalidate();
        }
    }

    /// <summary>Gets or sets plaintext content rendered in the bubble.</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string PlainText
    {
        get => _plainText;
        set
        {
            _plainText = value;
            RecalculateHeight();
            Invalidate();
        }
    }

    /// <summary>Gets or sets a value indicating whether this message belongs to the current user.</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool IsMine { get; set; }

    /// <summary>Gets or sets a value indicating whether this bubble is in a group chat.</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool IsGroup { get; set; }

    /// <inheritdoc />
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

        var hasAttachments = (_message?.Attachments.Count ?? 0) > 0;
        var text = string.IsNullOrWhiteSpace(_plainText)
            ? (hasAttachments ? string.Empty : _message?.EncryptedContent ?? string.Empty)
            : _plainText;
        var textSize = TextRenderer.MeasureText(text, new Font("Segoe UI", 10F), new Size(MaxBubbleWidth - HorizontalPadding * 2, 0), TextFormatFlags.WordBreak);
        var bubbleWidth = Math.Min(MaxBubbleWidth, Math.Max(90, textSize.Width + HorizontalPadding * 2));
        var bubbleHeight = Math.Max(42, textSize.Height + VerticalPadding * 2);
        var left = IsMine ? Width - bubbleWidth - 16 : 16;
        var bubbleRect = new Rectangle(left, 8, bubbleWidth, bubbleHeight);
        var bubbleColor = IsMine ? ThemeHelper.AccentBlue : ThemeHelper.BgCard;

        ThemeHelper.DrawRoundedRect(e.Graphics, bubbleRect, BubbleRadius, bubbleColor);
        TextRenderer.DrawText(
            e.Graphics,
            text,
            new Font("Segoe UI", 10F),
            new Rectangle(bubbleRect.Left + HorizontalPadding, bubbleRect.Top + VerticalPadding, bubbleRect.Width - HorizontalPadding * 2, bubbleRect.Height - VerticalPadding * 2),
            IsMine ? Color.White : ThemeHelper.TextPrimary,
            TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix);

        var attachmentBottom = DrawAttachments(e.Graphics, bubbleRect);

        var timestamp = _message?.SentAt.LocalDateTime.ToShortTimeString() ?? DateTime.Now.ToShortTimeString();
        var status = IsMine ? GetStatusIcon(_message?.Status ?? MessageStatus.Sending) : string.Empty;
        var footer = string.IsNullOrWhiteSpace(status) ? timestamp : $"{timestamp}  {status}";
        var footerTop = Math.Max(bubbleRect.Bottom, attachmentBottom) + 4;
        TextRenderer.DrawText(
            e.Graphics,
            footer,
            new Font("Segoe UI", 8F),
            new Rectangle(IsMine ? bubbleRect.Right - 110 : bubbleRect.Left, footerTop, 110, 18),
            _message?.Status == MessageStatus.Read ? ThemeHelper.AccentBlue : ThemeHelper.TextMuted,
            IsMine ? TextFormatFlags.Right : TextFormatFlags.Left);
    }

    /// <inheritdoc />
    protected override void OnMouseClick(MouseEventArgs e)
    {
        base.OnMouseClick(e);
        if (e.Button != MouseButtons.Left || _message is null)
        {
            return;
        }

        var attachment = _message.Attachments.FirstOrDefault();
        if (attachment is not null)
        {
            AttachmentClicked?.Invoke(this, attachment);
        }
    }

    private int DrawAttachments(Graphics graphics, Rectangle bubbleRect)
    {
        if (_message is null || _message.Attachments.Count == 0)
        {
            return bubbleRect.Bottom;
        }

        var top = bubbleRect.Bottom + 6;
        foreach (var attachment in _message.Attachments.Take(2))
        {
            var rect = new Rectangle(bubbleRect.Left, top, Math.Min(240, bubbleRect.Width), 34);
            ThemeHelper.DrawRoundedRect(graphics, rect, 8, IsMine ? Color.FromArgb(70, 132, 200) : ThemeHelper.BgHover);
            var text = attachment.MimeType.StartsWith("image/", StringComparison.OrdinalIgnoreCase)
                ? $"Image - {attachment.FileName}"
                : $"File - {attachment.FileName} ({FormatFileSize(attachment.FileSize)})";
            TextRenderer.DrawText(graphics, text, new Font("Segoe UI", 8.5F), rect, IsMine ? Color.White : ThemeHelper.TextPrimary, TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
            top += 40;
        }

        return top - 6;
    }

    private void RecalculateHeight()
    {
        var hasAttachments = (_message?.Attachments.Count ?? 0) > 0;
        var text = string.IsNullOrWhiteSpace(_plainText)
            ? (hasAttachments ? string.Empty : _message?.EncryptedContent ?? string.Empty)
            : _plainText;
        var textSize = TextRenderer.MeasureText(text, new Font("Segoe UI", 10F), new Size(MaxBubbleWidth - HorizontalPadding * 2, 0), TextFormatFlags.WordBreak);
        var attachmentHeight = ((_message?.Attachments.Count ?? 0) > 0) ? Math.Min(2, _message!.Attachments.Count) * 40 : 0;
        Height = Math.Max(68, textSize.Height + VerticalPadding * 2 + 54 + attachmentHeight);
    }

    private static string GetStatusIcon(MessageStatus status)
    {
        return status switch
        {
            MessageStatus.Sending => "...",
            MessageStatus.Sent => "✓",
            MessageStatus.Delivered => "✓✓",
            MessageStatus.Read => "✓✓",
            MessageStatus.Failed => "!",
            _ => string.Empty
        };
    }

    private static string FormatFileSize(long size)
    {
        if (size >= 1024 * 1024)
        {
            return $"{size / 1024D / 1024D:0.0} MB";
        }

        return $"{size / 1024D:0.0} KB";
    }

    private static ContextMenuStrip BuildContextMenu()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("Copy");
        menu.Items.Add("Reply");
        menu.Items.Add("Delete");
        return menu;
    }
}
