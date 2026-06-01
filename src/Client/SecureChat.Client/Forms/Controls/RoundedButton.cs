#nullable enable

using System.Drawing.Drawing2D;
using System.ComponentModel;
using SecureChat.Client.Forms.Helpers;

namespace SecureChat.Client.Forms.Controls;

/// <summary>
/// Represents a rounded button with hover feedback.
/// </summary>
public class RoundedButton : Button
{
    private bool _isHovered;

    /// <summary>
    /// Initializes a new instance of the <see cref="RoundedButton"/> class.
    /// </summary>
    public RoundedButton()
    {
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        BackColor = ThemeHelper.AccentBlue;
        ForeColor = Color.White;
        Cursor = Cursors.Hand;
        Height = 46;
        Radius = 8;
        HoverBackColor = ControlPaint.Light(ThemeHelper.AccentBlue, 0.12F);
        DisabledBackColor = ThemeHelper.BgHover;
        DisabledForeColor = ThemeHelper.TextMuted;
        Font = new Font("Segoe UI", 10F, FontStyle.Bold, GraphicsUnit.Point);
    }

    /// <summary>Gets or sets the corner radius.</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int Radius { get; set; }

    /// <summary>Gets or sets the hover background color.</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color HoverBackColor { get; set; }

    /// <summary>Gets or sets the disabled background color.</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color DisabledBackColor { get; set; }

    /// <summary>Gets or sets the disabled foreground color.</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color DisabledForeColor { get; set; }

    /// <inheritdoc />
    protected override void OnMouseEnter(EventArgs e)
    {
        _isHovered = true;
        Invalidate();
        base.OnMouseEnter(e);
    }

    /// <inheritdoc />
    protected override void OnMouseLeave(EventArgs e)
    {
        _isHovered = false;
        Invalidate();
        base.OnMouseLeave(e);
    }

    /// <inheritdoc />
    protected override void OnPaint(PaintEventArgs pevent)
    {
        pevent.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        var rect = new Rectangle(0, 0, Width - 1, Height - 1);
        var fill = Enabled ? (_isHovered ? HoverBackColor : BackColor) : DisabledBackColor;
        using var path = ThemeHelper.CreateRoundedRectanglePath(rect, Radius);
        using var brush = new SolidBrush(fill);
        pevent.Graphics.FillPath(brush, path);

        TextRenderer.DrawText(
            pevent.Graphics,
            Text,
            Font,
            rect,
            Enabled ? ForeColor : DisabledForeColor,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
    }
}
