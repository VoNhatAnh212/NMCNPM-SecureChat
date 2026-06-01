#nullable enable

using SecureChat.Client.Forms.Helpers;
using System.ComponentModel;

namespace SecureChat.Client.Forms.Controls;

/// <summary>
/// Represents a flat progress bar using the SecureChat palette.
/// </summary>
public class SecureProgressBar : Control
{
    private int _value;

    /// <summary>
    /// Initializes a new instance of the <see cref="SecureProgressBar"/> class.
    /// </summary>
    public SecureProgressBar()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
        Height = 8;
        Radius = 4;
        TrackColor = ThemeHelper.BgHover;
        ProgressColor = ThemeHelper.AccentBlue;
    }

    /// <summary>Gets or sets the progress value from 0 to 100.</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int Value
    {
        get => _value;
        set
        {
            _value = Math.Clamp(value, 0, 100);
            Invalidate();
        }
    }

    /// <summary>Gets or sets the track color.</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color TrackColor { get; set; }

    /// <summary>Gets or sets the progress color.</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color ProgressColor { get; set; }

    /// <summary>Gets or sets the corner radius.</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int Radius { get; set; }

    /// <inheritdoc />
    protected override void OnPaint(PaintEventArgs e)
    {
        var rect = new Rectangle(0, 0, Width - 1, Height - 1);
        ThemeHelper.DrawRoundedRect(e.Graphics, rect, Radius, TrackColor);

        var width = (int)Math.Round((Width - 1) * (_value / 100D));
        if (width <= 0)
        {
            return;
        }

        ThemeHelper.DrawRoundedRect(e.Graphics, new Rectangle(0, 0, width, Height - 1), Radius, ProgressColor);
    }
}
