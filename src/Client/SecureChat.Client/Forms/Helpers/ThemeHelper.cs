#nullable enable

using System.Drawing.Drawing2D;

namespace SecureChat.Client.Forms.Helpers;

/// <summary>
/// Provides shared colors and drawing helpers for SecureChat forms.
/// </summary>
public static class ThemeHelper
{
    /// <summary>Gets the primary dark background color.</summary>
    public static Color BgPrimary { get; private set; } = Color.FromArgb(18, 18, 18);

    /// <summary>Gets the secondary dark background color.</summary>
    public static Color BgSecondary { get; private set; } = Color.FromArgb(26, 26, 26);

    /// <summary>Gets the card background color.</summary>
    public static Color BgCard { get; private set; } = Color.FromArgb(36, 36, 36);

    /// <summary>Gets the hover background color.</summary>
    public static Color BgHover { get; private set; } = Color.FromArgb(46, 46, 46);

    /// <summary>Gets the primary accent blue color.</summary>
    public static Color AccentBlue { get; private set; } = Color.FromArgb(88, 166, 255);

    /// <summary>Gets the success accent green color.</summary>
    public static Color AccentGreen { get; private set; } = Color.FromArgb(63, 185, 80);

    /// <summary>Gets the primary text color.</summary>
    public static Color TextPrimary { get; private set; } = Color.FromArgb(240, 240, 240);

    /// <summary>Gets the secondary text color.</summary>
    public static Color TextSecondary { get; private set; } = Color.FromArgb(160, 160, 160);

    /// <summary>Gets the muted text color.</summary>
    public static Color TextMuted { get; private set; } = Color.FromArgb(100, 100, 100);

    /// <summary>Gets the danger color.</summary>
    public static Color Danger { get; private set; } = Color.FromArgb(248, 81, 73);

    /// <summary>Gets the border color.</summary>
    public static Color Border { get; private set; } = Color.FromArgb(48, 54, 61);

    /// <summary>
    /// Switches the palette to the dark theme.
    /// </summary>
    public static void UseDarkTheme()
    {
        BgPrimary = Color.FromArgb(18, 18, 18);
        BgSecondary = Color.FromArgb(26, 26, 26);
        BgCard = Color.FromArgb(36, 36, 36);
        BgHover = Color.FromArgb(46, 46, 46);
        AccentBlue = Color.FromArgb(88, 166, 255);
        AccentGreen = Color.FromArgb(63, 185, 80);
        TextPrimary = Color.FromArgb(240, 240, 240);
        TextSecondary = Color.FromArgb(160, 160, 160);
        TextMuted = Color.FromArgb(100, 100, 100);
        Danger = Color.FromArgb(248, 81, 73);
        Border = Color.FromArgb(48, 54, 61);
    }

    /// <summary>
    /// Switches the palette to a light theme.
    /// </summary>
    public static void UseLightTheme()
    {
        BgPrimary = Color.FromArgb(246, 248, 250);
        BgSecondary = Color.FromArgb(255, 255, 255);
        BgCard = Color.FromArgb(255, 255, 255);
        BgHover = Color.FromArgb(235, 240, 246);
        AccentBlue = Color.FromArgb(9, 105, 218);
        AccentGreen = Color.FromArgb(31, 136, 61);
        TextPrimary = Color.FromArgb(31, 35, 40);
        TextSecondary = Color.FromArgb(87, 96, 106);
        TextMuted = Color.FromArgb(111, 119, 130);
        Danger = Color.FromArgb(207, 34, 46);
        Border = Color.FromArgb(208, 215, 222);
    }

    /// <summary>
    /// Applies the active theme to a form.
    /// </summary>
    /// <param name="form">The target form.</param>
    public static void ApplyTheme(Form form)
    {
        form.BackColor = BgPrimary;
        form.ForeColor = TextPrimary;
        form.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
    }

    /// <summary>
    /// Draws a filled rounded rectangle.
    /// </summary>
    /// <param name="g">The graphics context.</param>
    /// <param name="rect">The rectangle bounds.</param>
    /// <param name="radius">The corner radius.</param>
    /// <param name="color">The fill color.</param>
    public static void DrawRoundedRect(Graphics g, Rectangle rect, int radius, Color color)
    {
        using var brush = new SolidBrush(color);
        using var path = CreateRoundedRectanglePath(rect, radius);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.FillPath(brush, path);
    }

    /// <summary>
    /// Draws a circular clipped image.
    /// </summary>
    /// <param name="g">The graphics context.</param>
    /// <param name="img">The image to draw.</param>
    /// <param name="rect">The target rectangle.</param>
    public static void DrawCircularImage(Graphics g, Image img, Rectangle rect)
    {
        using var path = new GraphicsPath();
        path.AddEllipse(rect);
        var previousClip = g.Clip;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.SetClip(path);
        g.DrawImage(img, rect);
        g.Clip = previousClip;
    }

    /// <summary>
    /// Creates a rounded rectangle path.
    /// </summary>
    /// <param name="rect">The rectangle bounds.</param>
    /// <param name="radius">The corner radius.</param>
    /// <returns>A rounded rectangle path.</returns>
    public static GraphicsPath CreateRoundedRectanglePath(Rectangle rect, int radius)
    {
        var path = new GraphicsPath();
        var diameter = Math.Max(1, radius * 2);
        var arc = new Rectangle(rect.Location, new Size(diameter, diameter));

        path.AddArc(arc, 180, 90);
        arc.X = rect.Right - diameter;
        path.AddArc(arc, 270, 90);
        arc.Y = rect.Bottom - diameter;
        path.AddArc(arc, 0, 90);
        arc.X = rect.Left;
        path.AddArc(arc, 90, 90);
        path.CloseFigure();
        return path;
    }
}
