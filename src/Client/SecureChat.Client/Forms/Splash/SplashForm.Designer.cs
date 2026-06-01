#nullable enable

using SecureChat.Client.Forms.Controls;
using SecureChat.Client.Forms.Helpers;

namespace SecureChat.Client.Forms.Splash;

/// <summary>
/// Designer portion of <see cref="SplashForm"/>.
/// </summary>
public partial class SplashForm
{
    private PictureBox _logoPictureBox = null!;
    private Label _titleLabel = null!;
    private Label _subtitleLabel = null!;
    private SecureProgressBar _progressBar = null!;
    private Label _statusLabel = null!;

    /// <summary>
    /// Releases resources used by the form.
    /// </summary>
    /// <param name="disposing">A value indicating whether managed resources should be disposed.</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _logoPictureBox?.Dispose();
        }

        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        SuspendLayout();

        _logoPictureBox = new PictureBox
        {
            Location = new Point(200, 34),
            Size = new Size(80, 80),
            BackColor = Color.Transparent
        };
        _logoPictureBox.Paint += LogoPictureBoxPaint;

        _titleLabel = new Label
        {
            AutoSize = false,
            Text = "SecureChat",
            Font = new Font("Segoe UI", 24F, FontStyle.Bold, GraphicsUnit.Point),
            ForeColor = ThemeHelper.TextPrimary,
            Location = new Point(40, 126),
            Size = new Size(400, 44),
            TextAlign = ContentAlignment.MiddleCenter
        };

        _subtitleLabel = new Label
        {
            AutoSize = false,
            Text = "Secure. Private. Yours.",
            Font = new Font("Segoe UI", 12F, FontStyle.Regular, GraphicsUnit.Point),
            ForeColor = ThemeHelper.TextMuted,
            Location = new Point(40, 172),
            Size = new Size(400, 26),
            TextAlign = ContentAlignment.MiddleCenter
        };

        _progressBar = new SecureProgressBar
        {
            Location = new Point(92, 224),
            Size = new Size(296, 8),
            Value = 8
        };

        _statusLabel = new Label
        {
            AutoSize = false,
            Text = "Starting...",
            Font = new Font("Segoe UI", 9.5F, FontStyle.Regular, GraphicsUnit.Point),
            ForeColor = ThemeHelper.TextMuted,
            Location = new Point(40, 244),
            Size = new Size(400, 24),
            TextAlign = ContentAlignment.MiddleCenter
        };

        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        BackColor = ThemeHelper.BgPrimary;
        ClientSize = new Size(480, 300);
        Controls.Add(_statusLabel);
        Controls.Add(_progressBar);
        Controls.Add(_subtitleLabel);
        Controls.Add(_titleLabel);
        Controls.Add(_logoPictureBox);
        FormBorderStyle = FormBorderStyle.None;
        Name = "SplashForm";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "SecureChat";

        ResumeLayout(false);
    }

    private static void LogoPictureBoxPaint(object? sender, PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        using var fill = new SolidBrush(ThemeHelper.AccentBlue);
        using var inner = new SolidBrush(ThemeHelper.BgPrimary);
        e.Graphics.FillEllipse(fill, new Rectangle(0, 0, 79, 79));
        e.Graphics.FillEllipse(inner, new Rectangle(18, 18, 44, 44));
        using var pen = new Pen(Color.White, 4F);
        e.Graphics.DrawArc(pen, new Rectangle(24, 28, 32, 24), 200, 140);
        e.Graphics.FillRectangle(Brushes.White, new Rectangle(25, 40, 30, 20));
    }
}
