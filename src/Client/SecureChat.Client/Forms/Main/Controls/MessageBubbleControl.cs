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
    private const int MinBubbleWidth = 96;
    private const int FooterHeight = 18;
    private const int FooterGap = 3;
    private const int AttachmentHeight = 38;
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

        if (_message is null)
        {
            return;
        }

        var metrics = MeasureBubble();
        var left = IsMine ? Width - metrics.Width - 16 : 16;
        var bubbleRect = new Rectangle(left, 8, metrics.Width, metrics.Height);
        var bubbleColor = IsMine ? ThemeHelper.AccentBlue : ThemeHelper.BgCard;

        ThemeHelper.DrawRoundedRect(e.Graphics, bubbleRect, BubbleRadius, bubbleColor);

        var contentTop = bubbleRect.Top + VerticalPadding;
        if (IsGroup && !IsMine && !string.IsNullOrWhiteSpace(_message.SenderName))
        {
            TextRenderer.DrawText(
                e.Graphics,
                _message.SenderName,
                new Font("Segoe UI", 8.5F, FontStyle.Bold),
                new Rectangle(bubbleRect.Left + HorizontalPadding, contentTop, bubbleRect.Width - HorizontalPadding * 2, 18),
                ThemeHelper.AccentGreen,
                TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
            contentTop += 20;
        }

        if (!string.IsNullOrWhiteSpace(metrics.Text))
        {
            TextRenderer.DrawText(
                e.Graphics,
                metrics.Text,
                new Font("Segoe UI", 10F),
                new Rectangle(bubbleRect.Left + HorizontalPadding, contentTop, bubbleRect.Width - HorizontalPadding * 2, metrics.TextHeight),
                IsMine ? Color.White : ThemeHelper.TextPrimary,
                TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix);
            contentTop += metrics.TextHeight + 6;
        }

        contentTop = DrawAttachments(e.Graphics, bubbleRect, contentTop);

        var timestamp = _message?.SentAt.LocalDateTime.ToShortTimeString() ?? DateTime.Now.ToShortTimeString();
        var status = IsMine ? GetStatusIcon(_message?.Status ?? MessageStatus.Sending) : string.Empty;
        var footer = string.IsNullOrWhiteSpace(status) ? timestamp : $"{timestamp}  {status}";
        var footerRect = new Rectangle(
            IsMine ? bubbleRect.Right - Math.Min(150, bubbleRect.Width) : bubbleRect.Left,
            bubbleRect.Bottom + FooterGap,
            Math.Min(150, bubbleRect.Width),
            FooterHeight);
        TextRenderer.DrawText(
            e.Graphics,
            footer,
            new Font("Segoe UI", 8F),
            footerRect,
            _message?.Status == MessageStatus.Read ? ThemeHelper.AccentBlue : ThemeHelper.TextMuted,
            (IsMine ? TextFormatFlags.Right : TextFormatFlags.Left) | TextFormatFlags.NoPrefix);
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

    /// <inheritdoc />
    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        RecalculateHeight();
    }

    private int DrawAttachments(Graphics graphics, Rectangle bubbleRect, int top)
    {
        if (_message is null || _message.Attachments.Count == 0)
        {
            return top;
        }

        foreach (var attachment in _message.Attachments.Take(2))
        {
            var rect = new Rectangle(bubbleRect.Left + HorizontalPadding, top, bubbleRect.Width - HorizontalPadding * 2, AttachmentHeight);
            ThemeHelper.DrawRoundedRect(graphics, rect, 8, IsMine ? Color.FromArgb(70, 132, 200) : ThemeHelper.BgHover);
            var text = attachment.MimeType.StartsWith("image/", StringComparison.OrdinalIgnoreCase)
                ? $"Image: {attachment.FileName}"
                : $"File: {attachment.FileName} ({FormatFileSize(attachment.FileSize)})";
            TextRenderer.DrawText(
                graphics,
                text,
                new Font("Segoe UI", 8.5F),
                new Rectangle(rect.Left + 10, rect.Top, rect.Width - 20, rect.Height),
                IsMine ? Color.White : ThemeHelper.TextPrimary,
                TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
            top += AttachmentHeight + 6;
        }

        return top;
    }

    private void RecalculateHeight()
    {
        Height = MeasureBubble().Height + FooterGap + FooterHeight + 14;
    }

    private BubbleMetrics MeasureBubble()
    {
        var hasAttachments = (_message?.Attachments.Count ?? 0) > 0;
        var text = string.IsNullOrWhiteSpace(_plainText)
            ? (hasAttachments ? string.Empty : _message?.EncryptedContent ?? string.Empty)
            : _plainText;
        var maxWidth = Math.Min(MaxBubbleWidth, Math.Max(220, (int)(Width * 0.72)));
        var contentWidth = maxWidth - HorizontalPadding * 2;
        var textSize = string.IsNullOrWhiteSpace(text)
            ? Size.Empty
            : TextRenderer.MeasureText(text, new Font("Segoe UI", 10F), new Size(contentWidth, int.MaxValue), TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix);

        var senderHeight = IsGroup && !IsMine && !string.IsNullOrWhiteSpace(_message?.SenderName) ? 20 : 0;
        var attachmentCount = Math.Min(2, _message?.Attachments.Count ?? 0);
        var attachmentBlockHeight = attachmentCount == 0 ? 0 : attachmentCount * (AttachmentHeight + 6);
        var attachmentWidth = attachmentCount == 0 ? 0 : 270;
        var bubbleWidth = Math.Min(maxWidth, Math.Max(MinBubbleWidth, Math.Max(textSize.Width + HorizontalPadding * 2, attachmentWidth + HorizontalPadding * 2)));
        var textHeight = string.IsNullOrWhiteSpace(text) ? 0 : Math.Max(18, textSize.Height);
        var textGap = textHeight > 0 && attachmentBlockHeight > 0 ? 6 : 0;
        var bubbleHeight = Math.Max(38, VerticalPadding * 2 + senderHeight + textHeight + textGap + attachmentBlockHeight);
        return new BubbleMetrics(text, textHeight, bubbleWidth, bubbleHeight);
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

    private readonly record struct BubbleMetrics(string Text, int TextHeight, int Width, int Height);
}
