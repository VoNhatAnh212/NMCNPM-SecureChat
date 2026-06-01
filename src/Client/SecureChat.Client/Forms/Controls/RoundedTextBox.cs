#nullable enable

using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Drawing.Drawing2D;
using SecureChat.Client.Forms.Helpers;

namespace SecureChat.Client.Forms.Controls;

/// <summary>
/// Represents a rounded text box with placeholder support.
/// </summary>
public class RoundedTextBox : UserControl
{
    private readonly TextBox _textBox;
    private bool _focused;

    /// <summary>
    /// Initializes a new instance of the <see cref="RoundedTextBox"/> class.
    /// </summary>
    public RoundedTextBox()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
        Radius = 8;
        BorderColor = ThemeHelper.Border;
        FocusBorderColor = ThemeHelper.AccentBlue;
        BackColor = ThemeHelper.BgCard;
        ForeColor = ThemeHelper.TextPrimary;
        Height = 44;
        Padding = new Padding(14, 10, 14, 6);

        _textBox = new TextBox
        {
            BorderStyle = BorderStyle.None,
            BackColor = BackColor,
            ForeColor = ForeColor,
            Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point),
            Location = new Point(Padding.Left, Padding.Top),
            Width = Width - Padding.Horizontal,
            Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right
        };

        _textBox.TextChanged += (_, _) => OnTextChanged(EventArgs.Empty);
        _textBox.GotFocus += (_, _) =>
        {
            _focused = true;
            Invalidate();
        };
        _textBox.LostFocus += (_, _) =>
        {
            _focused = false;
            Invalidate();
        };
        Controls.Add(_textBox);
    }

    /// <summary>Gets the inner text box control.</summary>
    [Browsable(false)]
    public TextBox InnerTextBox => _textBox;

    /// <summary>Gets or sets the text value.</summary>
    [AllowNull]
    public override string Text
    {
        get => _textBox.Text;
        set => _textBox.Text = value ?? string.Empty;
    }

    /// <summary>Gets or sets placeholder text.</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string PlaceholderText
    {
        get => _textBox.PlaceholderText;
        set => _textBox.PlaceholderText = value;
    }

    /// <summary>Gets or sets a value indicating whether text entry is password masked.</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool UseSystemPasswordChar
    {
        get => _textBox.UseSystemPasswordChar;
        set => _textBox.UseSystemPasswordChar = value;
    }

    /// <summary>Gets or sets the maximum number of characters.</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int MaxLength
    {
        get => _textBox.MaxLength;
        set => _textBox.MaxLength = value;
    }

    /// <summary>Gets or sets horizontal text alignment.</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public HorizontalAlignment TextAlign
    {
        get => _textBox.TextAlign;
        set => _textBox.TextAlign = value;
    }

    /// <summary>Gets or sets the corner radius.</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int Radius { get; set; }

    /// <summary>Gets or sets the border color.</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color BorderColor { get; set; }

    /// <summary>Gets or sets the focused border color.</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color FocusBorderColor { get; set; }

    /// <inheritdoc />
    protected override void OnBackColorChanged(EventArgs e)
    {
        base.OnBackColorChanged(e);
        if (_textBox is not null)
        {
            _textBox.BackColor = BackColor;
        }
    }

    /// <inheritdoc />
    protected override void OnForeColorChanged(EventArgs e)
    {
        base.OnForeColorChanged(e);
        if (_textBox is not null)
        {
            _textBox.ForeColor = ForeColor;
        }
    }

    /// <inheritdoc />
    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        if (_textBox is not null)
        {
            _textBox.Location = new Point(Padding.Left, Math.Max(6, (Height - _textBox.Height) / 2));
            _textBox.Width = Width - Padding.Horizontal;
        }
    }

    /// <inheritdoc />
    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        var rect = new Rectangle(0, 0, Width - 1, Height - 1);
        using var path = ThemeHelper.CreateRoundedRectanglePath(rect, Radius);
        using var fill = new SolidBrush(BackColor);
        using var border = new Pen(_focused ? FocusBorderColor : BorderColor, 1F);
        e.Graphics.FillPath(fill, path);
        e.Graphics.DrawPath(border, path);
    }
}
