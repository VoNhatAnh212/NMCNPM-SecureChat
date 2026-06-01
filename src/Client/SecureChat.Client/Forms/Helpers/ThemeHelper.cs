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
        BgPrimary = Color.FromArgb(17, 24, 39);
        BgSecondary = Color.FromArgb(24, 33, 48);
        BgCard = Color.FromArgb(31, 42, 59);
        BgHover = Color.FromArgb(43, 58, 82);
        AccentBlue = Color.FromArgb(96, 165, 250);
        AccentGreen = Color.FromArgb(52, 211, 153);
        TextPrimary = Color.FromArgb(248, 250, 252);
        TextSecondary = Color.FromArgb(203, 213, 225);
        TextMuted = Color.FromArgb(148, 163, 184);
        Danger = Color.FromArgb(248, 113, 113);
        Border = Color.FromArgb(71, 85, 105);
    }

    /// <summary>
    /// Switches the palette to a light theme.
    /// </summary>
    public static void UseLightTheme()
    {
        BgPrimary = Color.FromArgb(248, 250, 252);
        BgSecondary = Color.FromArgb(241, 245, 249);
        BgCard = Color.FromArgb(255, 255, 255);
        BgHover = Color.FromArgb(226, 232, 240);
        AccentBlue = Color.FromArgb(37, 99, 235);
        AccentGreen = Color.FromArgb(5, 150, 105);
        TextPrimary = Color.FromArgb(15, 23, 42);
        TextSecondary = Color.FromArgb(51, 65, 85);
        TextMuted = Color.FromArgb(100, 116, 139);
        Danger = Color.FromArgb(220, 38, 38);
        Border = Color.FromArgb(203, 213, 225);
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
        ApplyThemeTree(form);
    }

    /// <summary>
    /// Applies the active palette to a control tree.
    /// </summary>
    public static void ApplyThemeTree(Control root)
    {
        ApplyControlColors(root);
        foreach (Control child in root.Controls)
        {
            ApplyThemeTree(child);
        }

        root.Invalidate(true);
    }

    /// <summary>
    /// Applies the active palette to all open forms.
    /// </summary>
    public static void ApplyThemeToOpenForms()
    {
        foreach (Form form in Application.OpenForms)
        {
            ApplyTheme(form);
        }
    }

    private static void ApplyControlColors(Control control)
    {
        switch (control)
        {
            case Form:
            case UserControl:
                control.BackColor = BgPrimary;
                control.ForeColor = TextPrimary;
                break;
            case Panel or TableLayoutPanel or FlowLayoutPanel:
                control.BackColor = BgPrimary;
                control.ForeColor = TextPrimary;
                break;
            case TextBoxBase:
            case ListBox:
            case ListView:
            case ComboBox:
                control.BackColor = BgCard;
                control.ForeColor = TextPrimary;
                break;
            case Button button:
                button.BackColor = BgCard;
                button.ForeColor = TextPrimary;
                button.FlatStyle = FlatStyle.Flat;
                button.FlatAppearance.BorderColor = Border;
                break;
            case Label:
            case CheckBox:
            case RadioButton:
                control.BackColor = Color.Transparent;
                control.ForeColor = TextPrimary;
                break;
            default:
                control.ForeColor = TextPrimary;
                break;
        }
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
